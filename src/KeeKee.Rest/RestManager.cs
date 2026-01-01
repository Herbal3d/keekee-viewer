// Copyright 2025 Robert Adams
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.IO;
using System.Net;
using System.Text;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using KeeKee.Framework.Config;
using KeeKee.Framework.Logging;
using KeeKee.Framework;

namespace KeeKee.Rest {

    /// <summary>
    /// RestManager makes available HTTP connections to static and dynamic information.
    /// RestManager provides two functions: static web pages for ui and script support and
    /// REST interface capabilities for services within KeeKee to get and present data.
    /// 
    /// The static interface presents two sets of URLs which are mapped into the filesystem:
    /// http://127.0.0.1:9144/std/xxx : 'standard' pages which are common libraries
    /// This maps to the directory "Rest.Manager.UIContentDir" which defaults to
    /// "BINDIR/../UI/std/"
    /// http://127.0.0.1:9144/static/xxx : ui pages which can be 'skinned'
    /// This maps to the directory "Rest.Manager.UIContentDir"/"Rest.Manaager.Skin" which
    /// defaults to "BINDIR/../UI/Default/".
    /// 
    /// The dynamic content is created by servers creating instances of RestHandler.
    /// This creates URLs like:
    /// http://127.0.0.1:9144/api/SERVICE/xxx
    /// where 'service' is the name of teh service and 'xxx' is whatever it wants.
    /// These implement GET and POST operations of JSON formatted data.
    /// </summary>
    public class RestManager : BackgroundService {

        private readonly IKLogger<RestManager> m_log;
        private readonly IOptions<RestManagerConfig> m_config;
        private readonly IOptions<KeeKeeConfig> m_keeKeeConfig;

        public const string MIMEDEFAULT = "text/html";

        public int Port { get; private set; }
        private HttpListener? m_listener;
        private Thread? m_listenerThread;
        List<RestHandler> m_handlers = new List<RestHandler>();

        private readonly RestHandlerFactory m_RestHandlerFactory;

#pragma warning disable 414 // I know these are set and never referenced
        private IRestHandler? m_staticHandler;
        private IRestHandler? m_stdHandler;
        private IRestHandler? m_faviconHandler;
#pragma warning restore 414

        // Some system wide rest handlers to make information available
        private IRestHandler? m_workQueueRestHandler;
        private IRestHandler? m_paramDefaultRestHandler;
        private IRestHandler? m_paramIniRestHandler;
        private IRestHandler? m_paramUserRestHandler;
        private IRestHandler? m_paramOverrideRestHandler;

        // return the full base URL with the port added
        public readonly string BaseURL;

        public RestManager(IKLogger<RestManager> pLog,
                        IOptions<RestManagerConfig> pConfig,
                        IOptions<KeeKeeConfig> pKeeKeeConfig,
                        RestHandlerFactory pRestHandlerFactory) {
            m_log = pLog;
            m_config = pConfig;
            m_keeKeeConfig = pKeeKeeConfig;
            m_RestHandlerFactory = pRestHandlerFactory;

            BaseURL = pConfig.Value.BaseURL + ":" + pConfig.Value.Port.ToString();
            Port = pConfig.Value.Port;

            m_log.Log(KLogLevel.RestDetail, "RestManager constructor");
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken) {
            m_log.Log(KLogLevel.RestDetail, "RestManager ExecuteAsync entered");

            m_listener = new HttpListener();
            m_listener.Prefixes.Add(BaseURL + "/");

            // m_baseUIDIR = "/.../bin/KeeKeeUI/"
            string baseUIDir = m_config.Value.UIContentDir;
            if (!baseUIDir.EndsWith("/")) baseUIDir += "/";

            // things referenced as static are from the skinning directory below the UI dir
            // m_staticDir = "/.../bin/KeeKeeUI/Default/"
            string staticDir = baseUIDir;
            if (m_config.Value.Skin != null && m_config.Value.Skin.Length > 0) {
                string skinName = m_config.Value.Skin;
                skinName = skinName.Replace("/", "");  // skin names shouldn't fool with directories
                skinName = skinName.Replace("\\", "");
                skinName = skinName.Replace("..", "");
                staticDir = staticDir + skinName;
            }
            if (!staticDir.EndsWith("/")) staticDir += "/";

            // stdDir = "/.../bin/KeeKeeUI/std/";
            string stdDir = baseUIDir + "std/";

            m_log.Log(KLogLevel.RestDetail, "Registering FileHandler {0} -> {1}", "/static/", staticDir);
            m_staticHandler = m_RestHandlerFactory.Create("/static/", staticDir);

            m_log.Log(KLogLevel.RestDetail, "Registering FileHandler {0} -> {1}", "/std/", stdDir);
            m_stdHandler = m_RestHandlerFactory.Create("/std/", stdDir);
            m_faviconHandler = m_RestHandlerFactory.Create("/favicon.ico", baseUIDir);

            // some Framework structures that can be referenced
            // m_log.Log(KLogLevel.RestDetail, "Registering work queue stats at 'api/stats/workQueues'");
            // m_workQueueRestHandler = new RestHandler("/stats/workQueues", WorkQueueManager.Instance);

            // m_log.Log(KLogLevel.DINITDETAIL, "Registering parmeters at 'api/params/Default' and 'Ini', 'User', 'Override'");
            // m_paramDefaultRestHandler = new RestHandler("/params/Default", m_lgb.AppParams.DefaultParameters);
            // m_paramIniRestHandler = new RestHandler("/params/Ini", m_lgb.AppParams.IniParameters);
            // m_paramUserRestHandler = new RestHandler("/params/User", m_lgb.AppParams.UserParameters);
            // m_paramOverrideRestHandler = new RestHandler("/params/Override", m_lgb.AppParams.OverrideParameters);

            m_log.Log(KLogLevel.RestDetail, "Start(). Starting listening");
            m_listener.Start();

            while (cancellationToken.IsCancellationRequested == false) {
                await m_listener.GetContextAsync().ContinueWith((task) => {
                    try {
                        HttpListenerContext context = task.Result;
                        HttpListenerRequest request = context.Request;
                        HttpListenerResponse response = context.Response;

                        string absURL = request.Url?.AbsolutePath.ToLower() ?? "";
                        m_log.Log(KLogLevel.RestDetail, "HTTP request for {0}", absURL);

                        RestHandler? thisHandler = m_handlers.Find((rh) => absURL.StartsWith(rh.Prefix.ToLower()));

                        if (thisHandler != null) {
                            string afterString = absURL.Substring(thisHandler.Prefix.Length);
                            thisHandler.ProcessGetOrPostRequest(context, request, response, afterString);
                        } else {
                            m_log.Log(KLogLevel.Warning, "Request not processed because no matching handler, URL={0}", absURL);
                        }
                    } catch (Exception e) {
                        m_log.Log(KLogLevel.Error, "RestManager listener exception: {0}", e.ToString());
                    }
                }, cancellationToken);
            }

            // TODO: cleanup on exit
            m_log.Log(KLogLevel.RestDetail, "RestManager ExecuteAsync exiting");
            return;
        }

        public void RegisterListener(RestHandler handler) {
            m_log.Log(KLogLevel.RestDetail, "Registering prefix {0}", handler.Prefix);
            m_handlers.Add(handler);
        }

        #region HTML Helper Routines

        public delegate byte[] ConstructResponseBody();

        /// <summary>
        /// Construct and return the HTTP response
        /// </summary>
        /// <param name="pResponse">The response structure</param>
        /// <param name="pContentType">The MIME type of the response</param>
        /// <param name="pContentBodySource">Routine to call to generate the content body</param>
        public void DoSimpleResponse(HttpListenerResponse? pResponse,
                        string? pContentType,
                        ConstructResponseBody? pContentBodySource) {

            if (pResponse == null) return;

            byte[] encodedBuff;

            try {
                pResponse.ContentType = pContentType == null ? MIMEDEFAULT : pContentType;
                pResponse.AddHeader("Server", m_keeKeeConfig.Value.AppName);
                pResponse.AddHeader("Cache-Control", "no-cache");
                // context.Connection = ConnectionType.Close;

                encodedBuff = pContentBodySource != null ? pContentBodySource() : new byte[0];

                pResponse.StatusCode = (int)HttpStatusCode.OK;
            } catch {
                encodedBuff = OneNullBody();
                pResponse.StatusCode = (int)HttpStatusCode.InternalServerError;
            }

            // byte[] encodedBuff = System.Text.Encoding.UTF8.GetBytes(buff.ToString());

            pResponse.ContentLength64 = encodedBuff.Length;
            Stream output = pResponse.OutputStream;
            output.Write(encodedBuff, 0, encodedBuff.Length);
            output.Close();

            return;
        }

        /// <summary>
        /// Construct a response that is all abbout errors
        /// </summary>
        /// <param name="context">The request information</param>
        /// <param name="errCode">The HTTP error code toreturn</param>
        /// <param name="addContent">Called to add HTML to the body. May be null.</param>
        public void DoErrorResponse(HttpListenerResponse? pResponse,
                        HttpStatusCode errCode,
                        ConstructResponseBody? pContentBodySource) {

            if (pResponse == null) return;

            byte[] encodedBuff;

            try {
                pResponse.ContentType = MIMEDEFAULT;
                pResponse.AddHeader("Server", m_keeKeeConfig.Value.AppName);
                // context.Connection = ConnectionType.Close;

                encodedBuff = pContentBodySource != null ? pContentBodySource() : new byte[0];

                pResponse.StatusCode = (int)errCode;
            } catch {
                encodedBuff = OneNullBody();
                pResponse.StatusCode = (int)HttpStatusCode.InternalServerError;
            }

            // Ways to turn a StringBuilder into a byte array:
            // byte[] encodedBuff = System.Text.Encoding.UTF8.GetBytes(buff.ToString());
            // buff.Append(OMVSD.OSDParser.SerializeJsonString(resp));

            pResponse.ContentLength64 = encodedBuff.Length;
            System.IO.Stream output = pResponse.OutputStream;
            output.Write(encodedBuff, 0, encodedBuff.Length);
            output.Close();
            return;
        }

        /// <summary>
        /// Return a simple HTML body with nothing in it.
        /// </summary>
        /// <returns></returns>
        private byte[] OneNullBody() {
            var buff = new StringBuilder();
            buff.Append("<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Transitional//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd\">\r\n");
            buff.Append("<html xmlns=\"http://www.w3.org/1999/xhtml\">\r\n");
            buff.Append("<head></head><body></body></html>\r\n");
            return System.Text.Encoding.UTF8.GetBytes(buff.ToString());
        }


        #endregion HTML Helper Routines

    }
}