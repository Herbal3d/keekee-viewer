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

using KeeKee.Contexts;
using KeeKee.Framework;
using KeeKee.Framework.Logging;
using KeeKee.Rest;
using KeeKee.Renderer;

using OMV = OpenMetaverse;
using OMVSD = OpenMetaverse.StructuredData;

namespace KeeKee.View {
    /// <summary>
    /// Watch the comings and goings of the regions and handle the level of detail
    /// that the regions are displayed in.
    /// </summary>
    public class RegionTracker : IDisposable {

        private KLogger<RegionTracker> m_log;
        private bool m_enabled = false;

        private RestHandlerFactory m_restFactory;
        protected RestHandlerDisplayable? m_regionRestHandler;

        protected IWorld m_world;
        protected IRenderProvider m_renderer;

        public RegionTracker(KLogger<RegionTracker> pLog,
                            IWorld pWorld,
                            RestHandlerFactory pRestFactory,
                            IRenderProvider pRenderer) {
            m_log = pLog;
            m_world = pWorld;
            m_restFactory = pRestFactory;
            m_renderer = pRenderer;

            m_log.Log(KLogLevel.DINIT, "starting");

            if (m_enabled) {
                m_world.OnWorldRegionNew += new WorldRegionNewCallback(World_OnWorldRegionNew);
                m_world.OnWorldRegionRemoved += new WorldRegionRemovedCallback(World_OnWorldRegionRemoved);
                m_world.OnWorldRegionUpdated += new WorldRegionUpdatedCallback(World_OnWorldRegionUpdated);

                m_regionRestHandler = ((RestHandlerDisplayable)m_restFactory.CreateHandler<RestHandlerDisplayable>());
                m_regionRestHandler.Prefix = "/region/tracker/info";
                m_regionRestHandler.DisplayableSource = new RegionInformation(this);
            }
        }

        public void Dispose() {
            if (m_enabled) {
                m_world.OnWorldRegionNew -= new WorldRegionNewCallback(World_OnWorldRegionNew);
                m_world.OnWorldRegionRemoved -= new WorldRegionRemovedCallback(World_OnWorldRegionRemoved);
                m_world.OnWorldRegionUpdated -= new WorldRegionUpdatedCallback(World_OnWorldRegionUpdated);
            }
        }

        #region EVENT PROCESSING
        void World_OnWorldRegionNew(IRegionContext rcontext) {
            /*
            // we have a new region. Set the focus region to be where the main agent points
            if (World.World.Instance.Agent != null) {
                if (World.World.Instance.Agent.AssociatedAvatar != null) {
                    if (World.World.Instance.Agent.AssociatedAvatar.RegionContext != null) {
                        if (m_renderer != null) {
                            LogManager.Log.Log(LogLevel.DWORLDDETAIL, "RegionTracker: setting focus region {0}", 
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
        }
        void World_OnWorldRegionUpdated(IRegionContext rcontext, UpdateCodes what) {
        }
        #endregion EVENT PROCESSING

        #region RESPONSE DATA CONTSRUCTION
        private class RegionInformation : IDisplayable {
            RegionTracker m_tracker;
            public RegionInformation(RegionTracker regTrack) {
                m_tracker = regTrack;
            }
            public OMVSD.OSDMap GetDisplayable() {
                return new OMVSD.OSDMap();
            }
        }
        #endregion RESPONSE DATA CONSTRUCTION

    }
}
