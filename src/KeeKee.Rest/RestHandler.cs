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

using System.Net;
using System.Text;

using KeeKee.Framework;
using KeeKee.Framework.Logging;
using KeeKee.Framework.Utilities;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using OMV = OpenMetaverse;
using OMVSD = OpenMetaverse.StructuredData;

namespace KeeKee.Rest {

    public class RestHandlerFactory {
        private readonly ServiceProvider m_serviceProvider;

        public RestHandlerFactory(ServiceProvider pServiceProvider) {
            m_serviceProvider = pServiceProvider;
        }

        public IRestHandler CreateHandler<T>(params object[] parameters) where T : IRestHandler {
            return ActivatorUtilities.CreateInstance<T>(m_serviceProvider, parameters);
        }
    }

    public class RestHandler : IRestHandler, IDisposable {

        // public const string APINAME = "/api";

        // public const string RESTREQUESTERRORCODE = "RESTequestError";
        // public const string RESTREQUESTERRORMSG = "RESTRequestMsg";

        public string BaseUrl { get; private set; }
        public ProcessGetCallback? ProcessGet { get; private set; }
        public ProcessPostCallback? ProcessPost { get; private set; }
        public IDisplayable Displayable { get; private set; } = null!;
        public string Dir { get; private set; } = null!;
        public bool ParameterSetWritable { get; private set; } = false;
        public string Prefix { get; private set; } = null!;

        // Context of the current request to be used by ProcessGetCallback and ProcessPostCallback.
        public HttpListenerContext? ListenerContext { get; set; }
        public HttpListenerRequest? ListenerRequest { get; set; }
        public HttpListenerResponse? ListenerResponse { get; set; }

        private readonly RestManager m_restManager;
        private readonly IKLogger<RestHandler> m_log;
        private readonly IOptions<RestManagerConfig> m_options;

        /// <summary>
        /// Setup a rest handler that calls back for gets and posts to the specified urlBase.
        /// The 'urlBase' is like "/login/". This will create the rest interface "^/api/login/"
        /// </summary>
        /// <param name="urlBase">base of the url that's us</param>
        /// <param name="pget">called on GET operations</param>
        /// <param name="ppost">called on POST operations</param>
        public RestHandler(IKLogger<RestHandler> pLog,
                        RestManager pRestManager,
                        IOptions<RestManagerConfig> pOptions,
                        string urlBase, ProcessGetCallback? pget, ProcessPostCallback? ppost) {
            m_log = pLog;
            m_restManager = pRestManager;
            m_options = pOptions;
            BaseUrl = urlBase;
            ProcessGet = pget;
            ProcessPost = ppost;

            Prefix = Utilities.JoinFilePieces(m_options.Value.APIBase, BaseUrl);

            m_restManager.RegisterListener(this);
            m_log.Log(KLogLevel.RestDetail, "Register GET/POST handler for {0}", Prefix);
        }

        /// <summary>
        /// Setup a REST handler that returns the values from a IDisplayable instance.
        /// </summary>
        /// <param name="urlBase">base of the url for this parameter set</param>
        /// <param name="parms">the parameter set to read and write</param>
        /// <param name="writable">if 'true', it allows POST operations to change the parameter set</param>
        public RestHandler(IKLogger<RestHandler> pLog,
                        RestManager pRestManager,
                        IOptions<RestManagerConfig> pOptions,
                        string urlBase, IDisplayable displayable) {
            m_log = pLog;
            m_restManager = pRestManager;
            m_options = pOptions;
            BaseUrl = urlBase;
            Displayable = displayable;

            Prefix = Utilities.JoinFilePieces(m_options.Value.APIBase, BaseUrl);

            ProcessGet = ProcessGetParam;
            ProcessPost = null;
            m_restManager.RegisterListener(this);
            m_log.Log(KLogLevel.RestDetail, "Register GET/POST displayable handler for {0}", Prefix);
        }

        /// <summary>
        /// Setup a REST handler that returns the contents of a file
        /// </summary>
        /// <param name="urlBase"></param>
        /// <param name="directory"></param>
        public RestHandler(IKLogger<RestHandler> pLog,
                        RestManager pRestManager,
                        IOptions<RestManagerConfig> pOptions,
                        string urlBase, string directory) {
            m_log = pLog;
            m_restManager = pRestManager;
            m_options = pOptions;
            BaseUrl = urlBase;
            Dir = directory;

            Prefix = Utilities.JoinFilePieces(m_options.Value.APIBase, BaseUrl);

            m_restManager.RegisterListener(this);
            m_log.Log(KLogLevel.RestDetail, "Register GET/POST displayable handler for {0}", Prefix);
        }

        public void Dispose() {
            // TODO: figure out what should be done here
            // m_restManager.UnregisterListener(this);
        }

        /// <summary>
        /// Process an incoming GET or POST request.
        /// The URI is of the form "/api/BASE/afterString" where BASE is the BaseUrl.
        /// This comes from the RestManager which has already parsed off the "/api/BASE" part
        /// and passes us the 'afterString'.
        /// </summary>
        /// <param name="afterString"></param>
        /// <exception cref="KeeKeeException"></exception>
        public virtual void ProcessGetOrPostRequest(
            HttpListenerContext pContext, HttpListenerRequest pRequest, HttpListenerResponse pResponse,
                                                    string afterString) {
            Uri requestUrl = pRequest?.Url ?? new Uri("http://localhost/unknown");

            // GET processing
            if (pRequest?.HttpMethod.ToUpper().Equals("GET") ?? false) {
                try {
                    // Request for a file from a directory?
                    if (ProcessGet == null && Dir != null) {
                        // no processor but we have a dir. Return the file in that dir.
                        string filename = Dir + "/" + afterString;
                        if (File.Exists(filename)) {
                            // m_log.Log(KLogLevel.RestDetail, "GET: file: {0}", afterString);
                            string[] fileContents = File.ReadAllLines(filename);
                            string mimeType = RestManager.MIMEDEFAULT;
                            if (filename.EndsWith(".css")) mimeType = "text/css";
                            if (filename.EndsWith(".json")) mimeType = "text/json";
                            if (filename.EndsWith(".html")) mimeType = "text/html";
                            if (filename.EndsWith(".js")) mimeType = "text/javascript";
                            m_restManager.DoSimpleResponse(pResponse, mimeType,
                                    () => { return File.ReadAllBytes(filename); }
                            );
                        } else {
                            m_log.Log(KLogLevel.RestDetail, "GET: file does not exist: {0}", filename);
                        }
                        return;
                    }
                    // Not our special file case. Use the GET processor.
                    if (ProcessGet == null) {
                        throw new KeeKeeException("HTTP GET with no processing routine");
                    }
                    // m_log.Log(LogLevel.DRESTDETAIL, "GET: " + ListenerRequest.Url);
                    OMVSD.OSD resp = ProcessGet(this, requestUrl, afterString, pContext, pRequest, pResponse);
                    m_restManager.DoSimpleResponse(pResponse, "text/json",
                        () => {
                            return System.Text.Encoding.UTF8.GetBytes(OMVSD.OSDParser.SerializeJsonString(resp));
                        }
                    );
                } catch (Exception e) {
                    m_log.Log(KLogLevel.Error, "Failed getHandler: u="
                            + (pRequest?.Url?.ToString() ?? "UNKNOWN") + ":" + e.ToString());
                    m_restManager.DoErrorResponse(pResponse, HttpStatusCode.InternalServerError,
                        () => {
                            StringBuilder buff = new StringBuilder();
                            buff.Append("<html>\r\n");
                            buff.Append("<body>\r\n");
                            buff.Append("<div>");
                            buff.Append("FAILED GETTING '" + (pRequest?.Url?.ToString() ?? "UNKNOWN") + "'");
                            buff.Append("</div>");
                            buff.Append("<div>");
                            buff.Append("ERROR = '" + e.ToString() + "'");
                            buff.Append("</div>");
                            buff.Append("</body>\r\n");
                            buff.Append("</html>\r\n\r\n");

                            return System.Text.Encoding.UTF8.GetBytes(buff.ToString());
                        }
                    );
                }
                return;
            }
            if (pRequest?.HttpMethod.ToUpper().Equals("POST") ?? false) {
                m_log.Log(KLogLevel.RestDetail, "POST: " + (pRequest?.Url?.ToString() ?? "UNKNOWN"));
                string strBody = "";
                using (StreamReader rdr = new StreamReader(pRequest.InputStream)) {
                    strBody = rdr.ReadToEnd();
                    // m_log.Log(LogLevel.DRESTDETAIL, "APIPostHandler: Body: '" + strBody + "'");
                }
                try {
                    if (ProcessPost == null) {
                        throw new KeeKeeException("HTTP POST with no processing routine");
                    }
                    OMVSD.OSD body = MapizeTheBody(strBody);
                    OMVSD.OSD resp = ProcessPost(this, requestUrl, afterString, body, pContext, pRequest, pResponse);
                    if (resp != null) {
                        m_restManager.DoSimpleResponse(pResponse, "text/json",
                        () => {
                            return System.Text.Encoding.UTF8.GetBytes(OMVSD.OSDParser.SerializeJsonString(resp));
                        }
                        );
                    } else {
                        m_log.Log(KLogLevel.RestDetail, "Failure which creating POST response");
                        throw new KeeKeeException("Failure processing POST");
                    }
                } catch (Exception e) {
                    m_log.Log(KLogLevel.RestDetail, "Failed postHandler: u="
                            + (pRequest?.Url?.ToString() ?? "UNKNOWN") + ":" + e.ToString());
                    m_restManager.DoErrorResponse(pResponse, HttpStatusCode.InternalServerError,
                        () => {
                            StringBuilder buff = new StringBuilder();
                            buff.Append("<html>\r\n");
                            buff.Append("<body>\r\n");
                            buff.Append("<div>");
                            buff.Append("FAILED GETTING '" + (pRequest?.Url?.ToString() ?? "UNKNOWN") + "'");
                            buff.Append("</div>");
                            buff.Append("<div>");
                            buff.Append("ERROR = '" + e.ToString() + "'");
                            buff.Append("</div>");
                            buff.Append("</body>\r\n");
                            buff.Append("</html>\r\n\r\n");

                            return System.Text.Encoding.UTF8.GetBytes(buff.ToString());
                        }
                    );
                    // make up a response
                }
                return;
            }
        }

        /// <summary>
        /// Process a GET of parameters from the Displayable instance.
        /// An HTTP GET comes in as "/api/MYNAME/param" where 'param' is optional.
        /// If 'param' is there, return just that parameter. Otherwise return the whole set
        /// </summary>
        /// <param name="handler"></param>
        /// <param name="uri"></param>
        /// <param name="afterString"></param>
        /// <param name="pContext"></param>
        /// <param name="pRequest"></param>
        /// <param name="pResponse"></param>
        /// <returns></returns>
        public OMVSD.OSD ProcessGetParam(IRestHandler handler, Uri uri, string afterString,
            HttpListenerContext pContext, HttpListenerRequest pRequest, HttpListenerResponse pResponse) {

            // TODO: implement setting parameters in IDisplayable
            // This is from the old ParameterSet based code and doesn't work with IOption

            OMVSD.OSD ret = new OMVSD.OSDMap();
            OMVSD.OSDMap paramValues;
            // if (handler.Displayable == null) {
            //     paramValues = handler.m_parameterSet.GetDisplayable();
            // } else {
            paramValues = handler.Displayable?.GetDisplayable() ?? new OMVSD.OSDMap();
            // }
            try {
                if (afterString.Length > 0) {
                    // look to see if asking for one particular value
                    OMVSD.OSD oneValue;
                    if (paramValues.TryGetValue(afterString, out oneValue)) {
                        ret = oneValue;
                    } else {
                        // asked for a specific value but we don't have one of those. return empty
                    }
                } else {
                    // didn't specify a name. Return the whole parameter set
                    ret = paramValues;
                }
            } catch (Exception e) {
                m_log.Log(KLogLevel.RestDetail, "Failed fetching GetParam value: {0}", e);
            }
            return ret;
        }

        /// <summary>
        /// Process a POST to set parameters in the ParameterSet.
        /// An HTTP POST comes in as "/api/MYNAME/" with a body of key=value&key=value
        /// or a JSON formatted body.
        /// </summary>
        /// <param name="handler"></param>
        /// <param name="uri"></param>
        /// <param name="afterString"></param>
        /// <param name="rawbody"></param>
        /// <param name="pContext"></param>
        /// <param name="pRequest"></param>
        /// <param name="pResponse"></param>
        /// <returns></returns>
        public OMVSD.OSD ProcessPostParam(IRestHandler handler, Uri uri, string afterString, OMVSD.OSD rawbody,
            HttpListenerContext pContext, HttpListenerRequest pRequest, HttpListenerResponse pResponse) {

            // TODO: implement setting parameters in IDisplayable
            // This is from the old ParameterSet based code and doesn't work with IOption

            OMVSD.OSD ret = new OMVSD.OSDMap();
            /* Old code that could change parameters directly. Doesn't work with IOption
            try {
                OMVSD.OSDMap body = (OMVSD.OSDMap)rawbody;
                foreach (string akey in body.Keys) {
                    if (handler.m_parameterSet.HasParameter(akey)) {
                        handler.m_parameterSet.Update(akey, body[akey]);
                    }
                }
                ret = handler.m_parameterSet.GetDisplayable();
            } catch (Exception e) {
                m_log.Log(KLogLevel.RestDetail, "Failed setting param in POST: {0}", e);
            }
            */
            return ret;
        }
    }
}
