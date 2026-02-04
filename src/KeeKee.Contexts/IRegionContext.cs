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

using OMV = OpenMetaverse;

namespace KeeKee.Contexts {
    public abstract class IRegionContext : IEntity, IDisposable {

        #region Events
        public delegate void RegionRegionStateChangedCallback(IRegionContext pRegion, RegionStateCode code);
        public delegate void RegionRegionUpdatedCallback(IRegionContext pRegion, UpdateCodes what);
#pragma warning disable 0067   // disable unused event warning
        // when the underlying simulator is changing.
        public event RegionRegionStateChangedCallback? OnRegionStateChange;
        public event RegionRegionUpdatedCallback? OnRegionUpdated;
#pragma warning restore 0067
        #endregion

        protected WorldGroupCode m_worldGroup;
        public WorldGroupCode WorldGroup { get { return m_worldGroup; } }

        private RegionStateChangedCallback m_regionStateChangedCallback;
        public RegionState State { get; private set; }

        public IEntityCollection Entities { get; private set; }

        public IRegionContext(IKLogger pLog,
                                IWorld pWorld,
                                IEntityCollection pEntityCollection,
                                RegionState pRegionState,
                                IRegionContext? pRContext,  // null only for creating the region context itself
                                IAssetContext pAcontext)
                    : base(pLog, pWorld, pRContext, pAcontext, EntityClassifications.RegionContext) {

            State = pRegionState;
            Entities = pEntityCollection;

            // What state changes, pass it on
            m_regionStateChangedCallback = new RegionStateChangedCallback(State_OnChange);
            State.OnStateChanged += m_regionStateChangedCallback;
        }

        private void State_OnChange(RegionStateCode newState) {
            OnRegionStateChange?.Invoke(this, newState);
        }

        protected OMV.Vector3 m_size = new OMV.Vector3(256f, 256f, 8000f);
        public OMV.Vector3 Size { get { return m_size; } }

        // the world coordinate of the region's {0,0,0}
        protected OMV.Vector3d m_worldBase = new OMV.Vector3d(0d, 0d, 0d);
        public OMV.Vector3d WorldBase { get { return m_worldBase; } }

        // given an address relative to this region, return a global, world address
        public OMV.Vector3d CalculateGlobalPosition(OMV.Vector3 pos) {
            return m_worldBase + new OMV.Vector3d(pos.X, pos.Y, pos.Z);
        }
        public OMV.Vector3d CalculateGlobalPosition(float x, float y, float z) {
            return m_worldBase + new OMV.Vector3d(x, y, z);
        }

        // information on terrain for this region
        public ITerrainInfo? TerrainInfo { get; protected set; }

        // try and get an entity from the entity collection in this region
        public virtual bool TryGetEntity(EntityName entName, out IEntity? foundEnt) {
            return Entities.TryGetEntity(entName, out foundEnt);
        }

        public override void Update(UpdateCodes what) {
            base.Update(what);      // this sends an EntityUpdate for the region
            OnRegionUpdated?.Invoke(this, what);
        }

        public override void Dispose() {
            TerrainInfo = null; // let the garbage collector work
            if (State != null && m_regionStateChangedCallback != null) {
                State.OnStateChanged -= m_regionStateChangedCallback;
            }
            return;
        }
    }
}