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

using Microsoft.Extensions.Hosting;

using KeeKee.Contexts;
using KeeKee.Framework;
using KeeKee.Framework.Logging;
using KeeKee.Framework.Utilities;
using KeeKee.Rest;
using KeeKee.Renderer;

using OMV = OpenMetaverse;
using OMVSD = OpenMetaverse.StructuredData;

namespace KeeKee.World.Services {
    /// <summary>
    /// Watch the comings and goings of the regions and handle the level of detail
    /// that the regions are displayed in.
    /// </summary>
    public class RegionTracker : BackgroundService, IDisplayable, IDisposable {

        private KLogger<RegionTracker> m_log;

        private RestManager m_restManager;
        private RestHandlerFactory m_restFactory;
        protected RestHandler? m_regionRestHandler;

        protected IWorld m_world;
        protected IRenderProvider m_renderer;

        protected Dictionary<string, IRegionContext> m_regions;

        public RegionTracker(KLogger<RegionTracker> pLog,
                            IWorld pWorld,
                            RestHandlerFactory pRestFactory,
                            RestManager pRestManager,
                            IRenderProvider pRenderer) {
            m_log = pLog;
            m_world = pWorld;
            m_restFactory = pRestFactory;
            m_restManager = pRestManager;
            m_renderer = pRenderer;

            m_regions = new Dictionary<string, IRegionContext>();

            m_log.Log(KLogLevel.DINIT, "starting");

        }
        protected override async Task ExecuteAsync(CancellationToken cancellationToken) {
            m_log.Log(KLogLevel.DREST, "ExecuteAsync entered");

            m_regionRestHandler = m_restFactory.CreateHandlerDisplayable(
                Utilities.JoinFilePieces(m_restManager.APIBase, "/regions"), this);

            m_world.OnWorldRegionNew += World_OnWorldRegionNew;
            m_world.OnWorldRegionRemoved += World_OnWorldRegionRemoved;
            m_world.OnWorldRegionUpdated += World_OnWorldRegionUpdated;
        }

        public override void Dispose() {
            m_world.OnWorldRegionNew -= World_OnWorldRegionNew;
            m_world.OnWorldRegionRemoved -= World_OnWorldRegionRemoved;
            m_world.OnWorldRegionUpdated -= World_OnWorldRegionUpdated;
            base.Dispose();
        }

        #region EVENT PROCESSING
        void World_OnWorldRegionNew(IRegionContext rcontext) {
            m_log.Log(KLogLevel.DWORLD, $@"New region {rcontext.Name}");
            lock (m_regions) {
                if (!m_regions.ContainsKey(rcontext.Name.Name)) {
                    m_regions.Add(rcontext.Name.Name, rcontext);
                }
            }
            /*
            // we have a new region. Set the focus region to be where the main agent points
            if (World.World.Instance.Agent != null) {
                if (World.World.Instance.Agent.AssociatedAvatar != null) {
                    if (World.World.Instance.Agent.AssociatedAvatar.RegionContext != null) {
                        if (m_renderer != null) {
                            m_log.Log(KLogLevel.DWORLDDETAIL, "RegionTracker: setting focus region {0}", 
                                World.World.Instance.Agent.AssociatedAvatar.RegionContext.Name);
                            m_renderer.SetFocusRegion(World.World.Instance.Agent.AssociatedAvatar.RegionContext);
                        }
                    }
                }
            }
             */
            // for the moment, any close by region is good enough for focus
            if (m_renderer != null) {
                m_log.Log(KLogLevel.DWORLDDETAIL, "RegionTracker: setting focus region {0}", rcontext.Name);
                m_renderer.SetFocusRegion(rcontext);
            }
        }
        void World_OnWorldRegionRemoved(IRegionContext rcontext) {
            m_log.Log(KLogLevel.DWORLD, $@"Region removed {rcontext.Name}");
            lock (m_regions) {
                if (m_regions.ContainsKey(rcontext.Name.Name)) {
                    m_regions.Remove(rcontext.Name.Name);
                }
            }
        }
        void World_OnWorldRegionUpdated(IRegionContext rcontext, UpdateCodes what) {
        }
        #endregion EVENT PROCESSING

        // Return the information about the regions in the world. This is used for debugging and testing.
        public OMVSD.OSD GetDisplayable() {
            OMVSD.OSDMap ret = new OMVSD.OSDMap();
            var regionsInfo = new OMVSD.OSDArray();
            lock (m_regions) {
                foreach (var kvp in m_regions) {
                    var regionInfo = kvp.Value.GetDisplayable();
                    if (regionInfo != null) {
                        regionsInfo.Add(regionInfo);
                    }
                }
            }
            ret["Regions"] = regionsInfo;
            return ret;
        }
    }
}
