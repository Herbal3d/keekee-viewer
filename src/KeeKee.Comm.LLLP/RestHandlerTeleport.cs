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
using KeeKee.Framework;
using KeeKee.Framework.Logging;
using KeeKee.Framework.Utilities;

using Microsoft.Extensions.Options;

using OMV = OpenMetaverse;
using OMVSD = OpenMetaverse.StructuredData;

namespace KeeKee.Rest {

    public class RestHandlerTeleport : IRestHandler {

        private readonly KLogger<RestHandlerTeleport> m_log;
        private readonly IOptions<RestManagerConfig> m_restConfig;
        private readonly RestManager m_RestManager;
        private readonly ICommProvider m_commProvider;
        private readonly IOptions<CommConfig> m_commConfig;

        /// <summary>
        /// </summary>

        // The prefix of the requested URL that is processed by this handler.
        public string Prefix { get; set; }

        public RestHandlerTeleport(KLogger<RestHandlerTeleport> pLogger,
                                IOptions<RestManagerConfig> pRestConfig,
                                IOptions<CommConfig> pCommConfig,
                                RestManager pRestManager,
                                ICommProvider pCommProvider
                                ) {
            m_log = pLogger;
            m_restConfig = pRestConfig;
            m_RestManager = pRestManager;
            m_commProvider = pCommProvider;
            m_commConfig = pCommConfig;

            Prefix = Utilities.JoinFilePieces(m_restConfig.Value.APIBase, "LLLP/teleport");

            m_RestManager.RegisterListener(this);
        }

        public async Task ProcessGetOrPostRequest(HttpListenerContext pContext,
                                           HttpListenerRequest pRequest,
                                           HttpListenerResponse pResponse,
                                           CancellationToken pCancelToken) {

            if (pRequest?.HttpMethod.ToUpper().Equals("GET") ?? false) {
                // TODO: Implement GET handling if needed
                m_RestManager.DoErrorResponse(pResponse, HttpStatusCode.NotImplemented, null);
            }
            if (pRequest?.HttpMethod.ToUpper().Equals("POST") ?? false) {
                m_log.Log(KLogLevel.RestDetail, "POST: " + (pRequest?.Url?.ToString() ?? "UNKNOWN"));

                string strBody = "";
                using (StreamReader rdr = new StreamReader(pRequest.InputStream)) {
                    strBody = rdr.ReadToEnd();
                    // m_log.Log(KLogLevel.RestDetail, "APIPostHandler: Body: '" + strBody + "'");
                }
                try {
                    OMVSD.OSDMap body = m_RestManager.MapizeTheBody(strBody);

                    if (body.ContainsKey("DESTINATION")) {
                        string destination = body["DESTINATION"].AsString();
                        m_log.Log(KLogLevel.RestDetail, "Teleport request to " + destination);

                        bool result = m_commProvider.StartTeleport(destination);

                        OMVSD.OSDMap respMap = new OMVSD.OSDMap();
                        if (result) {
                            respMap.Add("result", new OMVSD.OSDString("success"));
                            respMap.Add("message", new OMVSD.OSDString("Teleport initiated"));
                        } else {
                            respMap.Add("result", new OMVSD.OSDString("failure"));
                            respMap.Add("message", new OMVSD.OSDString("Teleport failed"));
                        }
                        byte[] respBytes = Encoding.UTF8.GetBytes(respMap.ToString());
                        m_RestManager.DoSimpleResponse(pResponse, "application/json", () => respBytes);
                    } else {
                        m_log.Log(KLogLevel.Error, "RestHandlerTeleport: No DESTINATION in request");
                        m_RestManager.DoErrorResponse(pResponse, HttpStatusCode.BadRequest,
                                        () => Encoding.UTF8.GetBytes("No DESTINATION specified"));
                    }
                } catch (Exception e) {
                    m_log.Log(KLogLevel.Error, "RestHandlerTeleport: Exception {0} trying to teleport", e.Message);
                    m_RestManager.DoErrorResponse(pResponse, HttpStatusCode.InternalServerError,
                                        () => Encoding.UTF8.GetBytes("Exception during teleport request"));
                }
            }
        }

        public void Dispose() {
            // m_RestManager.UnregisterListener(this);
        }

        // Optional displayable interface to get parameters from. Not used here.
        public OMVSD.OSDMap? GetDisplayable() {
            return null;
        }
    }
}
