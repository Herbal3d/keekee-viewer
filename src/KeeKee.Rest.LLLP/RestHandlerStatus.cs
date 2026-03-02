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
using KeeKee.Comm.LLLP;
using KeeKee.Config;
using KeeKee.Framework.Logging;
using KeeKee.Framework.Utilities;
using KeeKee.Framework.WorkQueue;
using KeeKee.World;
using KeeKee.World.LL;
using Microsoft.Extensions.Options;

using OMV = OpenMetaverse;
using OMVSD = OpenMetaverse.StructuredData;

namespace KeeKee.Rest.LLLP {

    public class RestHandlerStatus : RestHandler {

        private readonly KLogger<RestHandlerStatus> m_log;
        private readonly IOptions<RestManagerConfig> m_restConfig;
        private readonly ICommProvider m_commProvider;
        private readonly CommLLLP? m_commLLLP;
        private readonly IOptions<CommConfig> m_commConfig;
        private readonly IOptions<GridConfig> m_gridConfig;
        private readonly Grids m_grids;
        private readonly WorkQueueManager m_workQueueManager;

        /// <summary>
        /// </summary>

        public RestHandlerStatus(KLogger<RestHandlerStatus> pLogger,
                                IOptions<RestManagerConfig> pRestConfig,
                                IOptions<CommConfig> pCommConfig,
                                IOptions<GridConfig> pGridConfig,
                                Grids p_grids,
                                RestManager pRestManager,
                                WorkQueueManager pWorkQueueManager,
                                ICommProvider pCommProvider
                                ) : base(pRestManager,
                                Utilities.JoinFilePieces(pRestManager.APIBase, "LLLP/stats")) {
            m_log = pLogger;
            m_restConfig = pRestConfig;
            m_commProvider = pCommProvider;
            m_grids = p_grids;
            m_workQueueManager = pWorkQueueManager;
            // Since we're LLLP specific, get the underlying CommLLLP
            m_commLLLP = pCommProvider as CommLLLP;
            m_commConfig = pCommConfig;
            m_gridConfig = pGridConfig;
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

            responseMap["currentgrid"] = m_commLLLP?.LoggedInGridName ?? "unknown";
            responseMap["currentsim"] = m_commLLLP?.GridClient?.Network?.CurrentSim?.Name ?? "unknown";

            // Add the main avatar's info.
            // Eventually this will loop and return all avatars in the array.
            OMVSD.OSDMap avatarInfo = new OMVSD.OSDMap();
            var cmptAvatar = m_commLLLP?.MainAgent?.Cmpt<LLCmptAvatar>();
            if (cmptAvatar != null) {
                avatarInfo["first"] = cmptAvatar.First;
                avatarInfo["last"] = cmptAvatar.Last;
                avatarInfo["displayname"] = cmptAvatar.DisplayName;
            }
            var cmptAvatarLoc = m_commLLLP?.MainAgent?.Cmpt<LLCmptLocation>();
            if (cmptAvatarLoc != null) {
                var globalPos = cmptAvatarLoc.GlobalPosition;
                var localPos = cmptAvatarLoc.LocalPosition;
                avatarInfo["globalPos"] = globalPos.ToString();
                avatarInfo["globalx"] = globalPos.X;
                avatarInfo["globaly"] = globalPos.Y;
                avatarInfo["globalz"] = globalPos.Z;
                avatarInfo["localPos"] = localPos.ToString();
                avatarInfo["x"] = localPos.X;
                avatarInfo["y"] = localPos.Y;
                avatarInfo["z"] = localPos.Z;
            }
            OMVSD.OSDArray avatarArray = new OMVSD.OSDArray();
            avatarArray.Add(avatarInfo);

            responseMap["avatar"] = avatarArray;

            // Add in the comm config parameters
            OMVSD.OSDMap commConfig = new OMVSD.OSDMap();
            foreach (var param in m_commConfig.Value.GetType().GetProperties()) {
                var val = param.GetValue(m_commConfig.Value);
                if (val != null) {
                    commConfig.Add(param.Name, val.ToString() ?? "");
                }
            }
            responseMap["commconfig"] = commConfig;

            // The stats that the CommProvider can provide
            responseMap["commstats"] = m_commProvider.CommStatistics.GetDisplayable();

            // Add in the grid info
            OMVSD.OSDArray possibleGrids = new OMVSD.OSDArray();
            m_grids.ForEach((gd) => {
                if (gd.GridNick == m_commLLLP?.LoggedInGridName) {
                    responseMap["currentgrid_fullname"] = gd.GridName ?? "unknown";
                    responseMap["currentgrid_loginuri"] = gd.LoginURI ?? "unknown";
                    responseMap["currentgrid_platform"] = gd.Platform ?? "unknown";
                    responseMap["currentgrid_website"] = gd.WebSite ?? "unknown";
                }
                possibleGrids.Add(gd.GridNick);
            });
            responseMap["possiblegrids"] = possibleGrids;

            // Send the response
            m_RestManager.DoSimpleResponse(pResponse, "application/json", () => {
                return Encoding.UTF8.GetBytes(responseMap.ToString());
            });
        }
    }
}
