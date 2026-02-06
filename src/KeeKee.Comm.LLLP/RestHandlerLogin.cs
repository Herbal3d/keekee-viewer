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

using KeeKee.Comm;
using KeeKee.Config;
using KeeKee.Framework.Logging;
using KeeKee.Framework.Utilities;
using KeeKee.World;

using Microsoft.Extensions.Options;

using OMV = OpenMetaverse;
using OMVSD = OpenMetaverse.StructuredData;

namespace KeeKee.Rest {

    public class RestHandlerLogin : IRestHandler {

        private readonly KLogger<RestHandlerLogin> m_log;
        private readonly IOptions<RestManagerConfig> m_restConfig;
        private readonly RestManager m_RestManager;
        private readonly ICommProvider m_commProvider;
        private readonly IOptions<CommConfig> m_commConfig;
        private readonly Grids m_grids;

        /// <summary>
        /// </summary>

        // The prefix of the requested URL that is processed by this handler.
        public string Prefix { get; set; }

        public RestHandlerLogin(KLogger<RestHandlerLogin> pLogger,
                                IOptions<RestManagerConfig> pRestConfig,
                                IOptions<CommConfig> pCommConfig,
                                RestManager pRestManager,
                                Grids pGrids,
                                ICommProvider pCommProvider
                                ) {
            m_log = pLogger;
            m_restConfig = pRestConfig;
            m_RestManager = pRestManager;
            m_commProvider = pCommProvider;
            m_commConfig = pCommConfig;
            m_grids = pGrids;

            Prefix = Utilities.JoinFilePieces(m_restConfig.Value.APIBase, "LLLP/login");

            m_RestManager.RegisterListener(this);
        }

        public async Task ProcessGetOrPostRequest(HttpListenerContext pContext,
                                           HttpListenerRequest pRequest,
                                           HttpListenerResponse pResponse,
                                           CancellationToken pCancelToken) {

            if (pRequest == null || pResponse == null) {
                m_log.Log(KLogLevel.Error, "RestHandlerLogin: Null request or response in ProcessGetOrPostRequest");
                return;
            }
            if (pRequest?.HttpMethod.ToUpper().Equals("GET") ?? false) {
                // GET handling returns the server status and the available grids
                try {
                    OMVSD.OSDMap respMap = new OMVSD.OSDMap();
                    OMVSD.OSDArray gridArray = new OMVSD.OSDArray();
                    m_grids.ForEach((gd) => {
                        OMVSD.OSDMap gridMap = new OMVSD.OSDMap();
                        gridMap.Add("GridNick", new OMVSD.OSDString(gd.GridNick));
                        gridMap.Add("GridName", new OMVSD.OSDString(gd.GridName));
                        gridMap.Add("LoginURI", new OMVSD.OSDString(gd.LoginURI));
                        gridArray.Add(gridMap);
                    });
                    respMap.Add("grids", gridArray);

                    byte[] respBytes = Encoding.UTF8.GetBytes(respMap.ToString());
                    m_RestManager.DoSimpleResponse(pResponse, "application/json", () => respBytes);
                } catch (Exception e) {
                    m_log.Log(KLogLevel.Error, "RestHandlerLogin: Exception {0} trying to do GET login", e.Message);
                    m_RestManager.DoErrorResponse(pResponse, HttpStatusCode.InternalServerError, null);
                }
            }
            if (pRequest?.HttpMethod.ToUpper().Equals("POST") ?? false) {
                // POST handling does the login
                m_log.Log(KLogLevel.RestDetail, "POST: " + (pRequest?.Url?.ToString() ?? "UNKNOWN"));

                string strBody = "";
                using (StreamReader rdr = new StreamReader(pRequest.InputStream)) {
                    strBody = rdr.ReadToEnd();
                    // m_log.Log(KLogLevel.RestDetail, "APIPostHandler: Body: '" + strBody + "'");
                }
                try {
                    OMVSD.OSD body = m_RestManager.MapizeTheBody(strBody);
                    LoginParams loginParams = new LoginParams();
                    loginParams.FromOSD(body);

                    OMV.LoginResponseData result = await m_commProvider.StartLogin(loginParams);

                    OMVSD.OSDMap respMap = new OMVSD.OSDMap();
                    if (result != null && result.Success) {
                        respMap.Add("result", new OMVSD.OSDString("success"));
                        respMap.Add("message", new OMVSD.OSDString(result.Message));
                        respMap.Add("session_id", new OMVSD.OSDString(result.SessionID.ToString()));
                        respMap.Add("loginResp", result.ToString());
                    } else {
                        respMap.Add("result", new OMVSD.OSDString("failure"));
                        respMap.Add("message", new OMVSD.OSDString("Login information was null"));
                    }
                    byte[] respBytes = Encoding.UTF8.GetBytes(respMap.ToString());
                    m_RestManager.DoSimpleResponse(pResponse, "application/json", () => respBytes);
                } catch (Exception e) {
                    m_log.Log(KLogLevel.Error, "RestHandlerLogin: Exception {0} trying to do login", e.Message);
                    m_RestManager.DoErrorResponse(pResponse, HttpStatusCode.InternalServerError, null);
                }
            }
        }

        public void Dispose() {
            // m_RestManager.UnregisterListener(this);
        }
    }
}
