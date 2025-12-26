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

        public IRestHandler Create(string urlBase, ProcessGetCallback pget, ProcessPostCallback ppost) {
            return ActivatorUtilities.CreateInstance<IRestHandler>(m_serviceProvider, urlBase, pget, ppost);
        }
        public IRestHandler Create(string urlBase, IDisplayable pDisplayable) {
            return ActivatorUtilities.CreateInstance<IRestHandler>(m_serviceProvider, urlBase, pDisplayable);
        }
        public IRestHandler Create(string urlBase, string pDirectory) {
            return ActivatorUtilities.CreateInstance<IRestHandler>(m_serviceProvider, urlBase, pDirectory);
        }
    }

    public class RestHandler : IRestHandler, IDisposable {

        // public const string APINAME = "/api";

        // public const string RESTREQUESTERRORCODE = "RESTequestError";
        // public const string RESTREQUESTERRORMSG = "RESTRequestMsg";

        public HttpListener Handler { get; private set; } = null!;
        public string BaseUrl { get; private set; }
        public ProcessGetCallback? ProcessGet { get; private set; }
        public ProcessPostCallback? ProcessPost { get; private set; }
        public IDisplayable Displayable { get; private set; } = null!;
        public string Dir { get; private set; } = null!;
        public bool ParameterSetWritable { get; private set; } = false;
        public string Prefix { get; private set; } = null!;

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
                        string urlBase, ProcessGetCallback pget, ProcessPostCallback ppost) {
            m_log = pLog;
            m_restManager = pRestManager;
            m_options = pOptions;
            BaseUrl = urlBase;
            Prefix = IRestHandler.APINAME + urlBase;

            ProcessGet = pget;
            ProcessPost = ppost;

            m_restManager.RegisterListener(this);
            m_log.Log(KLogLevel.RestDetail, "Register GET/POST handler for {0}", Prefix);
        }

        /// <summary>
        /// Setup a REST handler that returns the values from a ParameterSet.
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
            Prefix = IRestHandler.APINAME + urlBase;

            ProcessGet = ProcessGetParam;
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
            Prefix = urlBase;

            m_restManager.RegisterListener(this);
            m_log.Log(KLogLevel.RestDetail, "Register GET/POST displayable handler for {0}", Prefix);
        }

        public void Dispose() {
            if (Handler != null) {
                Handler.Stop();
                Handler = null;
            }
        }

        public virtual void GetPostAsync(string afterString) {
            Uri requestUrl = ListenerRequest?.Url ?? new Uri("http://localhost/unknown");

            if (ListenerRequest?.HttpMethod.ToUpper().Equals("GET") ?? false) {
                if (ProcessGet == null && Dir != null) {
                    // no processor but we have a dir. Return the file in that dir.
                    string filename = Dir + "/" + afterString;
                    if (File.Exists(filename)) {
                        // m_log.Log(KLogLevel.RestDetail, "GET: file: {0}", afterString);
                        string[] fileContents = File.ReadAllLines(filename);
                        string mimeType = RestManager.MIMEDEFAULT;
                        if (filename.EndsWith(".css")) mimeType = "text/css";
                        if (filename.EndsWith(".json")) mimeType = "text/json";
                        m_restManager.ConstructSimpleResponse(ListenerResponse, mimeType,
                            delegate (ref StringBuilder buff) {
                                foreach (string line in fileContents) {
                                    buff.Append(line);
                                    buff.Append("\r\n");
                                }
                            }
                        );
                    } else {
                        m_log.Log(KLogLevel.RestDetail, "GET: file does not exist: {0}", filename);
                    }
                    return;
                }
                try {
                    if (ProcessGet == null) {
                        throw new KeeKeeException("HTTP GET with no processing routine");
                    }
                    // m_log.Log(LogLevel.DRESTDETAIL, "GET: " + ListenerRequest.Url);
                    OMVSD.OSD resp = ProcessGet(this, requestUrl, afterString);
                    m_restManager.ConstructSimpleResponse(ListenerResponse, "text/json",
                        delegate (ref StringBuilder buff) {
                            buff.Append(OMVSD.OSDParser.SerializeJsonString(resp));
                        }
                    );
                } catch (Exception e) {
                    m_log.Log(KLogLevel.Error, "Failed getHandler: u="
                            + (ListenerRequest?.Url?.ToString() ?? "UNKNOWN") + ":" + e.ToString());
                    m_restManager.ConstructErrorResponse(ListenerResponse, HttpStatusCode.InternalServerError,
                        delegate (ref StringBuilder buff) {
                            buff.Append("<div>");
                            buff.Append("FAILED GETTING '" + (ListenerRequest?.Url?.ToString() ?? "UNKNOWN") + "'");
                            buff.Append("</div>");
                            buff.Append("<div>");
                            buff.Append("ERROR = '" + e.ToString() + "'");
                            buff.Append("</div>");
                        }
                    );
                }
                return;
            }
            if (ListenerRequest?.HttpMethod.ToUpper().Equals("POST") ?? false) {
                m_log.Log(KLogLevel.RestDetail, "POST: " + (ListenerRequest?.Url?.ToString() ?? "UNKNOWN"));
                string strBody = "";
                using (StreamReader rdr = new StreamReader(ListenerRequest.InputStream)) {
                    strBody = rdr.ReadToEnd();
                    // m_log.Log(LogLevel.DRESTDETAIL, "APIPostHandler: Body: '" + strBody + "'");
                }
                try {
                    if (ProcessPost == null) {
                        throw new KeeKeeException("HTTP POST with no processing routine");
                    }
                    OMVSD.OSD body = MapizeTheBody(strBody);
                    OMVSD.OSD resp = ProcessPost(this, requestUrl, afterString, body);
                    if (resp != null) {
                        m_restManager.ConstructSimpleResponse(ListenerResponse, "text/json",
                            delegate (ref StringBuilder buff) {
                                buff.Append(OMVSD.OSDParser.SerializeJsonString(resp));
                            }
                        );
                    } else {
                        m_log.Log(KLogLevel.RestDetail, "Failure which creating POST response");
                        throw new KeeKeeException("Failure processing POST");
                    }
                } catch (Exception e) {
                    m_log.Log(KLogLevel.RestDetail, "Failed postHandler: u="
                            + (ListenerRequest?.Url?.ToString() ?? "UNKNOWN") + ":" + e.ToString());
                    m_restManager.ConstructErrorResponse(ListenerResponse, HttpStatusCode.InternalServerError,
                        delegate (ref StringBuilder buff) {
                            buff.Append("<div>");
                            buff.Append("FAILED GETTING '" + requestUrl.ToString() + "'");
                            buff.Append("</div>");
                            buff.Append("<div>");
                            buff.Append("ERROR = '" + e.ToString() + "'");
                            buff.Append("</div>");
                        }
                    );
                    // make up a response
                }
                return;
            }
        }

        private OMVSD.OSDMap MapizeTheBody(string body) {
            OMVSD.OSDMap retMap = new OMVSD.OSDMap();
            if (body.Length > 0 && body.Substring(0, 1).Equals("{")) { // kludge test for JSON formatted body
                try {
                    retMap = (OMVSD.OSDMap)OMVSD.OSDParser.DeserializeJson(body);
                } catch (Exception e) {
                    m_log.Log(KLogLevel.RestDetail, "Failed parsing of JSON body: " + e.ToString());
                }
            } else {
                try {
                    string[] amp = body.Split('&');
                    if (amp.Length > 0) {
                        foreach (string kvp in amp) {
                            string[] kvpPieces = kvp.Split('=');
                            if (kvpPieces.Length == 2) {
                                retMap.Add(kvpPieces[0].Trim(), new OMVSD.OSDString(kvpPieces[1].Trim()));
                            }
                        }
                    }
                } catch (Exception e) {
                    m_log.Log(KLogLevel.RestDetail, "Failed parsing of query body: " + e.ToString());
                }
            }
            return retMap;
        }

        public OMVSD.OSD ProcessGetParam(IRestHandler handler, Uri uri, string afterString) {
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

        public OMVSD.OSD ProcessPostParam(IRestHandler handler, Uri uri, string afterString, OMVSD.OSD rawbody) {
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
