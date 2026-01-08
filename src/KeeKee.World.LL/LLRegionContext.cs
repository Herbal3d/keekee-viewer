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
using OpenMetaverse;
using OMV = OpenMetaverse;

namespace KeeKee.World.LL {
    public sealed class LLRegionContext : RegionContextBase {
        private KLogger<LLRegionContext> m_log;
        public OMV.Simulator Simulator { get; private set; }

        private OMV.GridClient GridComm { get; set; }

        private Dictionary<uint, int> m_recentLocalIDRequests = new Dictionary<uint, int>();
        private LLInstanceFactory m_llInstanceFactory;

        public LLRegionContext(KLogger<LLRegionContext> pLog,
                                LLInstanceFactory pFactory,
                                IWorld pWorld,
                                IAssetContext pAContext,
                                OMV.GridClient pGridComm,
                                OMV.Simulator pSim)
                            : base(pLog, pWorld, null, pAContext) {
            m_log = pLog;
            m_llInstanceFactory = pFactory;
            GridComm = pGridComm;

            RegionContext = this;

            TerrainInfo = m_llInstanceFactory.Create<LLTerrainInfo>(this, pAContext);

            // until we have a better protocol, we know the sims are a fixed size
            m_size = new OMV.Vector3(256f, 256f, 8000f);

            // believe it or not the world coordinates of a sim are hidden in the handle
            uint x, y;
            OMV.Utils.LongToUInts(pSim.Handle, out x, out y);
            m_worldBase = new OMV.Vector3d((double)x, (double)y, 0d);

            this.Simulator = pSim;

            // this should be more general as "GRID/SIM"
            Name = new EntityName(pSim.Name);

            // a cache of requested localIDs so we don't ask too often
            m_recentLocalIDRequests = new Dictionary<uint, int>();
        }

        /// <summary>
        /// Called to request a particular local ID should be sent to us. Very LLLP dependent.
        /// This is rare enough  that we don't bother locking.
        /// </summary>
        /// <param name="localID"></param>
        public void RequestLocalID(uint localID) {
            int now = System.Environment.TickCount & 0x3fffffff;
            uint requestID = 0;
            // First some code that reduces the frequency of repeat requests
            lock (m_recentLocalIDRequests) {
                if (m_recentLocalIDRequests.ContainsKey(localID)) {
                    // we've asked for this localID recently. See how recent.
                    if (m_recentLocalIDRequests[localID] < now) {
                        // it was a while ago. Time to ask again
                        m_recentLocalIDRequests.Remove(localID);
                    }
                }
                if (!m_recentLocalIDRequests.ContainsKey(localID)) {
                    // remember the time when we should try again. Once per 5 seconds
                    m_recentLocalIDRequests.Add(localID, now + (5 * 1000));
                    requestID = localID;
                }
            }
            if (requestID != 0) {
                // send the packet outside the lock
                m_log.Log(KLogLevel.DCOMMDETAIL, "LLRegionContext.RequestLocalID: asking for {0}/{1}", this.Name, localID);
                GridComm.Objects.RequestObject(this.Simulator, localID);
            }
        }

        public bool TryGetEntityLocalID(uint localID, out IEntity ent) {
            // it's a kludge, but localID is the same as global ID
            // TODO: add some checking for rcontext since the localIDs are scoped by 'simulator'
            // we are relying on a low collision rate for localIDs
            // A linear search of the list takes way too long for the number of objects arriving
            return Entities.TryGetEntity((ulong)localID, out ent);
        }

        /// <summary>
        /// </summary>
        /// <param name="localID"></param>
        /// <param name="ent"></param>
        /// <param name="createIt"></param>
        /// <returns>true if we created a new entry</returns>
        public bool TryGetCreateEntityLocalID(uint localID, out IEntity? ent, RegionCreateEntityCallback createIt) {
            try {
                IEntity newEntity = null;
                lock (Entities) {
                    if (!TryGetEntityLocalID(localID, out ent)) {
                        newEntity = createIt();
                        Entities.AddEntity(newEntity);
                        ent = newEntity;
                    }
                }
                return true;
            } catch (Exception e) {
                m_log.Log(KLogLevel.DBADERROR, "TryGetCreateEntityLocalID: Failed to create entity: {0}", e.ToString());
            }
            ent = null;
            return false;
        }


        public override void Dispose() {
            base.Dispose();
            this.Simulator = null;
        }


    }
}
