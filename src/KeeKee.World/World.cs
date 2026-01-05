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

using KeeKee.Framework.Logging;
using Microsoft.Extensions.Hosting;
using OMV = OpenMetaverse;

namespace KeeKee.World {

    public sealed class World : BackgroundService, IWorld {
        private IKLogger m_log;

        private IAgent? m_agent = null;

        // list of the region information build for the simulator
        List<IRegionContext> m_regionList;

        // list of parameters and information on the grids
        Grids m_grids;

        #region Events
#pragma warning disable 0067   // disable unused event warning
        // A new region has been added to the world
        public event WorldRegionNewCallback OnWorldRegionNew;
        // A known region has changed it's state (terrain, location, ...)
        public event WorldRegionUpdatedCallback OnWorldRegionUpdated;
        // a region is removed from the world
        public event WorldRegionRemovedCallback OnWorldRegionRemoved;

        // when new items are added to the world
        public event WorldEntityNewCallback OnWorldEntityNew;
        // when an entity is updated
        public event WorldEntityUpdateCallback OnWorldEntityUpdate;
        // when an object is killed
        public event WorldEntityRemovedCallback OnWorldEntityRemoved;

        // When an agent is added to the world
        public event WorldAgentNewCallback OnAgentNew;
        // When an agent is added to the world
        public event WorldAgentUpdateCallback OnAgentUpdate;
        // When an agent is removed from the world
        public event WorldAgentRemovedCallback OnAgentRemoved;
#pragma warning restore 0067
        #endregion

        /// <summary>
        /// Constructor called in instance of main and not in own thread. This is only
        /// good for setting up structures.
        /// </summary>
        public World(KLogger<World> pLog) {
            m_log = pLog;

            m_regionList = new List<IRegionContext>();
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken) {
            m_log.Log(KLogLevel.DINIT, "World ExecuteAsync started.");
            // wait until cancelled
            try {
                while (!cancellationToken.IsCancellationRequested) {
                    await Task.Delay(1000, cancellationToken);
                }
            } catch (TaskCanceledException) {
                // normal exit path
            }
            m_log.Log(KLogLevel.DINIT, "World ExecuteAsync exiting.");
        }

        #region IWorld methods
        #region Region Management
        public void AddRegion(IRegionContext rcontext) {
            m_log.Log(KLogLevel.DWORLD, "Simulator connected " + rcontext.Name);
            IRegionContext foundRegion = null;
            lock (m_regionList) {
                foundRegion = GetRegion(rcontext.Name);
                if (foundRegion == null) {
                    // we don't know about this region. Add it and connect to events
                    m_regionList.Add(rcontext);

                    IEntityCollection coll;
                    if (rcontext.TryGet<IEntityCollection>(out coll)) {
                        if (Region_OnNewEntityCallback == null) {
                            Region_OnNewEntityCallback = new EntityNewCallback(Region_OnNewEntity);
                        }
                        coll.OnEntityNew += Region_OnNewEntityCallback;

                        if (Region_OnUpdateEntityCallback == null) {
                            Region_OnUpdateEntityCallback = new EntityUpdateCallback(Region_OnUpdateEntity);
                        }
                        coll.OnEntityUpdate += Region_OnUpdateEntityCallback;

                        if (Region_OnRemovedEntityCallback == null) {
                            Region_OnRemovedEntityCallback = new EntityRemovedCallback(Region_OnRemovedEntity);
                        }
                        coll.OnEntityRemoved += Region_OnRemovedEntityCallback;
                    }

                    if (Region_OnRegionUpdatedCallback == null) {
                        Region_OnRegionUpdatedCallback = new RegionRegionUpdatedCallback(Region_OnRegionUpdated);
                    }
                    rcontext.OnRegionUpdated += Region_OnRegionUpdatedCallback;
                }
            }
            // tell the world there is a new region (do it outside the lock)
            if (foundRegion == null) {
                if (OnWorldRegionNew != null) OnWorldRegionNew(rcontext);
            }
        }

        #region REGION EVENT PROCESSING
        private EntityNewCallback Region_OnNewEntityCallback = null;
        private void Region_OnNewEntity(IEntity ent) {
            m_log.Log(KLogLevel.DWORLDDETAIL, "Region_OnNewEntity: {0}", ent.Name.Name);
            if (OnWorldEntityNew != null) OnWorldEntityNew(ent);
            return;
        }

        private EntityUpdateCallback Region_OnUpdateEntityCallback = null;
        private void Region_OnUpdateEntity(IEntity ent, UpdateCodes what) {
            if (OnWorldEntityUpdate != null) OnWorldEntityUpdate(ent, what);
            return;
        }

        private EntityRemovedCallback Region_OnRemovedEntityCallback = null;
        private void Region_OnRemovedEntity(IEntity ent) {
            m_log.Log(KLogLevel.DWORLDDETAIL, "Region_OnRemovedEntity: {0}", ent.Name.Name);
            if (OnWorldEntityRemoved != null) OnWorldEntityRemoved(ent);
            return;
        }

        private RegionRegionUpdatedCallback Region_OnRegionUpdatedCallback = null;
        private void Region_OnRegionUpdated(IRegionContext rcontext, UpdateCodes what) {
            if (OnWorldRegionUpdated != null) OnWorldRegionUpdated(rcontext, what);
            return;
        }
        #endregion REGION EVENT PROCESSING

        public IRegionContext? GetRegion(EntityName name) {
            IRegionContext? ret = null;
            lock (m_regionList) {
                foreach (IRegionContext rcb in m_regionList) {
                    if (rcb.Name.Equals(name)) {
                        ret = rcb;
                        break;
                    }
                }
            }
            return ret;
        }

        public IRegionContext? FindRegion(Predicate<IRegionContext> pred) {
            IRegionContext? ret = null;
            lock (m_regionList) {
                foreach (IRegionContext rcb in m_regionList) {
                    if (pred(rcb)) {
                        ret = rcb;
                        break;
                    }
                }
            }
            return ret;
        }

        public void RemoveRegion(IRegionContext rcontext) {
            IRegionContext? foundRegion = null;
            lock (m_regionList) {
                foundRegion = GetRegion(rcontext.Name);
                if (foundRegion != null) {
                    // we know about this region so remove it and disconnect from events
                    m_regionList.Remove(foundRegion);
                    m_log.Log(KLogLevel.DWORLD, "Removing region " + foundRegion.Name);
                    IEntityCollection coll;
                    if (rcontext.TryGet<IEntityCollection>(out coll)) {
                        if (Region_OnNewEntityCallback != null) {
                            coll.OnEntityNew -= Region_OnNewEntityCallback;
                        }
                        if (Region_OnUpdateEntityCallback != null) {
                            coll.OnEntityUpdate -= Region_OnUpdateEntityCallback;
                        }
                        if (Region_OnRemovedEntityCallback != null) {
                            coll.OnEntityRemoved -= Region_OnRemovedEntityCallback;
                        }
                    }
                    if (Region_OnRegionUpdatedCallback != null) {
                        rcontext.OnRegionUpdated -= Region_OnRegionUpdatedCallback;
                    }
                    if (OnWorldRegionRemoved != null) OnWorldRegionRemoved(rcontext);
                } else {
                    m_log.Log(KLogLevel.DBADERROR, "RemoveRegion: asked to remove region we don't have. Name={0}", rcontext.Name);
                }
            }
        }

        #endregion Region Management

        /// <summary>
        /// A global call to find an entity. We ask all the regions if they have it.
        /// This is only here because the renderer looses the context for an entity
        /// when control passes into the renderer and then back. The renderer only
        /// has the name of the entity.
        /// </summary>
        /// <param name="entName">the name of the entity to look for</param>
        /// <param name="ent">place to store the reference to the found entity</param>
        /// <returns>'true' if entity found</returns>
        public bool TryGetEntity(EntityName entName, out IEntity? ent) {
            IEntity? ret = null;
            lock (m_regionList) {
                foreach (IRegionContext rcb in m_regionList) {
                    IEntityCollection coll;
                    if (rcb.TryGet<IEntityCollection>(out coll)) {
                        coll.TryGetEntity(entName, out ret);
                    }
                    if (ret != null) break;
                }
            }
            ent = ret;
            return (ret != null);
        }

        #region AGENT MANAGEMENT
        public IAgent? Agent { get { return m_agent; } }

        public void AddAgent(IAgent agnt) {
            m_log.Log(KLogLevel.DWORLD, "AddAgent: ");
            m_agent = agnt;
            if (OnAgentNew != null) OnAgentNew(agnt);
        }

        public void UpdateAgent(UpdateCodes what) {
            m_log.Log(KLogLevel.DWORLDDETAIL, "UpdateAgent: ");
            if (OnAgentUpdate != null && m_agent != null) OnAgentUpdate(m_agent, what);
        }

        public void RemoveAgent() {
            m_log.Log(KLogLevel.DWORLD, "RemoveAgent: ");
            if (m_agent != null) {
                if (OnAgentRemoved != null) OnAgentRemoved(m_agent);
                m_agent = null;
            }
        }

        void UpdateAgentCamera(IAgent agnt, OMV.Vector3 position, OMV.Quaternion direction) {
            return;
        }
        #endregion AGENT MANAGEMENT
        #endregion IWorld methods

        #region GRID MANAGEMENT
        public Grids Grids {
            get {
                if (m_grids == null) {
                    m_grids = new Grids();
                }
                return m_grids;
            }
        }
        #endregion GRID MANAGEMENT


    }
}
