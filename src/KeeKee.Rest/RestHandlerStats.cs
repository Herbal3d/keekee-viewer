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
using KeeKee.Contexts;
using KeeKee.Entity;
using KeeKee.Framework.Logging;
using KeeKee.Framework.Utilities;
using KeeKee.Framework.WorkQueue;
using KeeKee.World;
using Microsoft.Extensions.Options;

using OMV = OpenMetaverse;
using OMVSD = OpenMetaverse.StructuredData;

namespace KeeKee.Rest {

    public class RestHandlerStats : RestHandler {

        private readonly KLogger<RestHandlerStats> m_log;
        private readonly IOptions<RestManagerConfig> m_restConfig;
        private readonly ICommProvider m_commProvider;
        private readonly IOptions<CommConfig> m_commConfig;
        private readonly IOptions<GridConfig> m_gridConfig;
        private readonly WorkQueueManager m_workQueueManager;
        private readonly IWorld m_world;
        private readonly ComponentFactory m_ComponentFactory;

        /// <summary>
        /// </summary>

        public RestHandlerStats(KLogger<RestHandlerStats> pLogger,
                                IOptions<RestManagerConfig> pRestConfig,
                                IOptions<CommConfig> pCommConfig,
                                IOptions<GridConfig> pGridConfig,
                                RestManager pRestManager,
                                WorkQueueManager pWorkQueueManager,
                                ComponentFactory pComponentFactory,
                                ICommProvider pCommProvider,
                                IWorld pWorld
                                ) : base(pRestManager,
                                Utilities.JoinFilePieces(pRestManager.APIBase, "/stats")) {
            m_log = pLogger;
            m_restConfig = pRestConfig;
            m_commConfig = pCommConfig;
            m_gridConfig = pGridConfig;
            m_workQueueManager = pWorkQueueManager;
            m_ComponentFactory = pComponentFactory;
            m_commProvider = pCommProvider;
            m_world = pWorld;
        }

        public override async Task ProcessGetRequest(HttpListenerContext pContext,
                                           HttpListenerRequest pRequest,
                                           HttpListenerResponse pResponse,
                                           CancellationToken pCancelToken) {

            OMVSD.OSDMap responseMap = new OMVSD.OSDMap {
                ["status"] = "success",
                ["timestamp"] = DateTime.UtcNow.ToString("o"),
                ["commprovider"] = m_commProvider.GetType().Name,
                ["isconnected"] = m_commProvider.IsConnected,
                ["isloggedin"] = m_commProvider.IsLoggedIn
            };

            responseMap["workqueues"] = m_workQueueManager.GetDisplayable() ?? new OMVSD.OSDMap();

            responseMap["components"] = m_ComponentFactory.GetDisplayable() ?? new OMVSD.OSDMap();

            responseMap["world"] = m_world.GetDisplayable() ?? new OMVSD.OSDMap();

            /* Sample code on how configuration parameters can be added to the response.
            // Add in the comm config parameters
            OMVSD.OSDMap commConfig = new OMVSD.OSDMap();
            foreach (var param in m_commConfig.Value.GetType().GetProperties()) {
                var val = param.GetValue(m_commConfig.Value);
                if (val != null) {
                    commConfig[param.Name.ToLower()] = val.ToString() ?? "";
                }
            }
            responseMap["commconfig"] = commConfig;
            */

            // Send the response
            m_RestManager.DoSimpleResponse(pResponse, "application/json", () => {
                return Encoding.UTF8.GetBytes(responseMap.ToString());
            });

            if (pRequest?.HttpMethod.ToUpper().Equals("POST") ?? false) {
                m_log.Log(KLogLevel.DRESTDETAIL, "POST: " + (pRequest?.Url?.ToString() ?? "UNKNOWN"));
                m_RestManager.DoErrorResponse(pResponse, HttpStatusCode.NotImplemented, null);

            }
            ;
        }
    }
}

