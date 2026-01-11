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

using KeeKee.Comm;
using KeeKee.Config;
using KeeKee.Framework.Logging;
using KeeKee.Framework.Utilities;

using Microsoft.Extensions.Options;

using OMV = OpenMetaverse;
using OMVSD = OpenMetaverse.StructuredData;

namespace KeeKee.Rest {

    public class RestHandlerLogout : IRestHandler {

        private readonly KLogger<RestHandlerLogout> m_log;
        private readonly IOptions<RestManagerConfig> m_restConfig;
        private readonly RestManager m_RestManager;
        private readonly ICommProvider m_commProvider;
        private readonly IOptions<CommConfig> m_commConfig;

        /// <summary>
        /// </summary>

        // The prefix of the requested URL that is processed by this handler.
        public string Prefix { get; set; }

        public RestHandlerLogout(KLogger<RestHandlerLogout> pLogger,
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

            Prefix = Utilities.JoinFilePieces(m_restConfig.Value.APIBase, "LLLP/logout");

            m_RestManager.RegisterListener(this);
        }

        public async Task ProcessGetOrPostRequest(HttpListenerContext pContext,
                                           HttpListenerRequest pRequest,
                                           HttpListenerResponse pResponse,
                                           CancellationToken pCancelToken) {

            if (pRequest?.HttpMethod.ToUpper().Equals("GET") ?? false) {
                // TODO: Implement GET handling if needed
            }
            if (pRequest?.HttpMethod.ToUpper().Equals("POST") ?? false) {
                m_log.Log(KLogLevel.RestDetail, "POST: " + (pRequest?.Url?.ToString() ?? "UNKNOWN"));

                try {
                    m_commProvider.StartLogout();
                } catch (Exception e) {
                    m_log.Log(KLogLevel.RestDetail, "Logout exception: " + e.ToString());
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
