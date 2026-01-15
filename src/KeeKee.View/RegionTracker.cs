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

using KeeKee.Framework;
using KeeKee.Framework.Logging;
using KeeKee.Framework.Modules;
using KeeKee.Framework.Parameters;
using KeeKee.Rest;
using KeeKee.Renderer;
using KeeKee.World;

using OMV = OpenMetaverse;
using OMVSD = OpenMetaverse.StructuredData;

namespace KeeKee.View {
    /// <summary>
    /// Watch the comings and goings of the regions and handle the level of detail
    /// that the regions are displayed in.
    /// </summary>
    public class RegionTracker : IRegionTrackerProvider, IModule {

        protected RestHandler m_regionRestHandler;

        protected IWorld m_world;
        protected IRenderProvider m_renderer;

        #region IMODULE
        protected string m_moduleName;
        public string ModuleName { get { return m_moduleName; } set { m_moduleName = value; } }

        protected KeeKeeBase m_lgb = null;
        public KeeKeeBase LGB { get { return m_lgb; } }

        public IAppParameters ModuleParams { get { return m_lgb.AppParams; } }

        public RegionTracker() {
            // default to the class name. The module code can set it to something else later.
            m_moduleName = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name;
        }

        // IModule.OnLoad
        public virtual void OnLoad(string modName, KeeKeeBase lgbase) {
            LogManager.Log.Log(LogLevel.DINIT, "RegionTracker.OnLoad()");
            m_moduleName = modName;
            m_lgb = lgbase;
            // set the parameter defaults
            ModuleParams.AddDefaultParameter(ModuleName + ".Regions.Enable", "true",
                        "Whether to make region information available");
            ModuleParams.AddDefaultParameter(ModuleName + ".Renderer.Name", "Renderer",
                        "Name of renderer module for display of region details");
        }

        // IModule.AfterAllModulesLoaded
        public virtual bool AfterAllModulesLoaded() {
            LogManager.Log.Log(LogLevel.DINIT, "EntityTracker.AfterAllModulesLoaded()");
            // connect to the world and listen for entity events (there is only one world)
            m_world = World.World.Instance;
            string rendererName = ModuleParams.ParamString(ModuleName + ".Renderer.Name");
            m_renderer = (IRenderProvider)ModuleManager.Instance.Module(rendererName);
            if (ModuleParams.ParamBool(ModuleName + ".Regions.Enable")) {
                m_world.OnWorldRegionNew += new WorldRegionNewCallback(World_OnWorldRegionNew);
                m_world.OnWorldRegionRemoved += new WorldRegionRemovedCallback(World_OnWorldRegionRemoved);
                m_world.OnWorldRegionUpdated += new WorldRegionUpdatedCallback(World_OnWorldRegionUpdated);
            }

            if (ModuleParams.ParamBool(ModuleName + ".Regions.Enable")) {
                m_regionRestHandler = new RestHandler("/Tracker/Regions/", new RegionInformation(this));
            }
            return true;
        }

        // IModule.Start
        public virtual void Start() {
            return;
        }

        // IModule.Stop
        public virtual void Stop() {
            return;
        }

        // IModule.PrepareForUnload
        public virtual bool PrepareForUnload() {
            if (ModuleParams.ParamBool(ModuleName + ".Regions.Enable")) {
                m_world.OnWorldRegionNew -= new WorldRegionNewCallback(World_OnWorldRegionNew);
                m_world.OnWorldRegionRemoved -= new WorldRegionRemovedCallback(World_OnWorldRegionRemoved);
                m_world.OnWorldRegionUpdated -= new WorldRegionUpdatedCallback(World_OnWorldRegionUpdated);
            }
            return false;
        }
        #endregion IMODULE

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
                LogManager.Log.Log(LogLevel.DWORLDDETAIL, "RegionTracker: setting focus region {0}", rcontext.Name);
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
