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

        // public const string RESTREQUESTERRORCODE = "RESTRequestError";
        // public const string RESTREQUESTERRORMSG = "RESTRequestMsg";

        public HttpListener? m_Handler;
        public string BaseUrl { get; private set; }
        public ProcessGetCallback? ProcessGet { get; private set; }
        public ProcessPostCallback? ProcessPost { get; private set; }
        public IDisplayable Displayable { get; private set; } = null!;
        public string Dir { get; private set; } = null!;
        public bool ParameterSetWritable { get; private set; } = false;
        public string Prefix { get; private set; } = null!;

        public HttpListenerContext? ListenerContext { get; private set; }
        public HttpListenerRequest? ListenerRequest { get; private set; }
        public HttpListenerResponse? ListenerResponse { get; private set; }

        private readonly ServiceProvider m_serviceProvider;
        private readonly IKLogger<RestHandler> m_log;

        /// <summary>
        /// Setup a rest handler that calls back for gets and posts to the specified urlBase.
        /// The 'urlBase' is like "/login/". This will create the rest interface "^/api/login/"
        /// </summary>
        /// <param name="urlBase">base of the url that's us</param>
        /// <param name="pget">called on GET operations</param>
        /// <param name="ppost">called on POST operations</param>
        public RestHandler(IKLogger<RestHandler> pLog, ServiceProvider pServiceProvider,
                        string urlBase, ProcessGetCallback pget, ProcessPostCallback ppost) {
            m_log = pLog;
            m_serviceProvider = pServiceProvider;
            BaseUrl = urlBase;
            Prefix = APINAME + urlBase;

            ProcessGet = pget;
            ProcessPost = ppost;

            m_serviceProvider.GetRequiredService<RestManager>().RegisterListener(this);
            m_log.Log(KLogLevel.RestDetail, "Register GET/POST handler for {0}", Prefix);
        }

        /// <summary>
        /// Setup a REST handler that returns the values from a ParameterSet.
        /// </summary>
        /// <param name="urlBase">base of the url for this parameter set</param>
        /// <param name="parms">the parameter set to read and write</param>
        /// <param name="writable">if 'true', it allows POST operations to change the parameter set</param>
        public RestHandler(IKLogger<RestHandler> pLog, ServiceProvider pServiceProvider,
                        string urlBase, IDisplayable displayable) {
            m_log = pLog;
            m_serviceProvider = pServiceProvider;
            BaseUrl = urlBase;
            m_displayable = displayable;
            m_prefix = APINAME + urlBase;

            m_processGet = ProcessGetParam;
            m_serviceProvider.GetRequiredService<RestManager>().RegisterListener(this);
            m_log.Log(KLogLevel.RestDetail, "Register GET/POST displayable handler for {0}", m_prefix);
        }

        /// <summary>
        /// Setup a REST handler that returns the contents of a file
        /// </summary>
        /// <param name="urlBase"></param>
        /// <param name="directory"></param>
        public RestHandler(IKLogger<RestHandler> pLog, ServiceProvider pServiceProvider,
                        string urlBase, string directory) {
            m_log = pLog;
            m_serviceProvider = pServiceProvider;
            BaseUrl = urlBase;
            m_dir = directory;
            m_prefix = urlBase;

            m_serviceProvider.GetRequiredService<RestManager>().RegisterListener(this);
            m_log.Log(KLogLevel.RestDetail, "Register GET/POST displayable handler for {0}", m_prefix);
        }

        public void Dispose() {
            if (m_Handler != null) {
                m_Handler.Stop();
                m_Handler = null;
            }
        }

        public virtual void GetPostAsync(string afterString) {
            Uri requestUrl = m_request != null && m_request.Url != null ? m_request.Url : new Uri("http://localhost/unknown");

            if (m_request?.HttpMethod.ToUpper().Equals("GET") ?? false) {
                if (m_processGet == null && m_dir != null) {
                    // no processor but we have a dir. Return the file in that dir.
                    string filename = m_dir + "/" + afterString;
                    if (File.Exists(filename)) {
                        // m_log.Log(KLogLevel.RestDetail, "GET: file: {0}", afterString);
                        string[] fileContents = File.ReadAllLines(filename);
                        string mimeType = RestManager.MIMEDEFAULT;
                        if (filename.EndsWith(".css")) mimeType = "text/css";
                        if (filename.EndsWith(".json")) mimeType = "text/json";
                        RestManager.Instance.ConstructSimpleResponse(m_response, mimeType,
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
                    if (m_processGet == null) {
                        throw new KeeKeeException("HTTP GET with no processing routine");
                    }
                    // m_log.Log(LogLevel.DRESTDETAIL, "GET: " + m_request.Url);
                    OMVSD.OSD resp = m_processGet(this, requestUrl, afterString);
                    RestManager.Instance.ConstructSimpleResponse(m_response, "text/json",
                        delegate (ref StringBuilder buff) {
                            buff.Append(OMVSD.OSDParser.SerializeJsonString(resp));
                        }
                    );
                } catch (Exception e) {
                    m_log.Log(KLogLevel.Error, "Failed getHandler: u="
                            + (m_request?.Url?.ToString() ?? "UNKNOWN") + ":" + e.ToString());
                    RestManager.Instance.ConstructErrorResponse(m_response, HttpStatusCode.InternalServerError,
                        delegate (ref StringBuilder buff) {
                            buff.Append("<div>");
                            buff.Append("FAILED GETTING '" + (m_request?.Url?.ToString() ?? "UNKNOWN") + "'");
                            buff.Append("</div>");
                            buff.Append("<div>");
                            buff.Append("ERROR = '" + e.ToString() + "'");
                            buff.Append("</div>");
                        }
                    );
                }
                return;
            }
            if (m_request?.HttpMethod.ToUpper().Equals("POST") ?? false) {
                m_log.Log(KLogLevel.RestDetail, "POST: " + (m_request?.Url?.ToString() ?? "UNKNOWN"));
                string strBody = "";
                using (StreamReader rdr = new StreamReader(m_request.InputStream)) {
                    strBody = rdr.ReadToEnd();
                    // m_log.Log(LogLevel.DRESTDETAIL, "APIPostHandler: Body: '" + strBody + "'");
                }
                try {
                    if (m_processPost == null) {
                        throw new KeeKeeException("HTTP POST with no processing routine");
                    }
                    OMVSD.OSD body = MapizeTheBody(strBody);
                    OMVSD.OSD resp = m_processPost(this, requestUrl, afterString, body);
                    if (resp != null) {
                        RestManager.Instance.ConstructSimpleResponse(m_response, "text/json",
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
                            + (m_request?.Url?.ToString() ?? "UNKNOWN") + ":" + e.ToString());
                    RestManager.Instance.ConstructErrorResponse(m_response, HttpStatusCode.InternalServerError,
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

        public OMVSD.OSD ProcessGetParam(RestHandler handler, Uri uri, string afterString) {
            OMVSD.OSD ret = new OMVSD.OSDMap();
            OMVSD.OSDMap paramValues;
            // if (handler.m_displayable == null) {
            //     paramValues = handler.m_parameterSet.GetDisplayable();
            // } else {
            paramValues = handler.m_displayable?.GetDisplayable() ?? new OMVSD.OSDMap();
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

        /*
        public OMVSD.OSD ProcessPostParam(RestHandler handler, Uri uri, string afterString, OMVSD.OSD rawbody) {
            OMVSD.OSD ret = new OMVSD.OSDMap();
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
            return ret;
        }
        */
    }
}
