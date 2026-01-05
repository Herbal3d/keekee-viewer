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

using System;
using System.Collections.Generic;
using System.Text;

using KeeKee.Framework.Logging;

using OMV = OpenMetaverse;

namespace KeeKee.World {
    public abstract class RegionContextBase : EntityBase, IRegionContext, IDisposable {

        #region Events
#pragma warning disable 0067   // disable unused event warning
        // when the underlying simulator is changing.
        public event RegionRegionStateChangeCallback? OnRegionStateChange;
        public event RegionRegionUpdatedCallback? OnRegionUpdated;

#pragma warning restore 0067
        #endregion

        protected WorldGroupCode m_worldGroup;
        public WorldGroupCode WorldGroup { get { return m_worldGroup; } }

        private RegionStateChangedCallback m_regionStateChangedCallback;
        protected RegionState m_regionState;
        public RegionState State {
            get { return m_regionState; }
        }

        public RegionContextBase(IKLogger pLog,
                                IWorld pWorld,
                                IRegionContext pRContext,
                                IAssetContext pAcontext)
                    : base(pLog, pWorld, pRContext, pAcontext) {

            m_regionState = new RegionState();
            m_entityCollection = new EntityCollection(this.Name.Name);

            // What state changes, pass it on
            m_regionStateChangedCallback = new RegionStateChangedCallback(State_OnChange);
            State.OnStateChanged += m_regionStateChangedCallback;

            this.RegisterInterface<IEntityCollection>(m_entityCollection);
        }

        private void State_OnChange(RegionStateCode newState) {
            if (OnRegionStateChange != null) OnRegionStateChange(this, newState);
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
        protected TerrainInfoBase? m_terrainInfo = null;
        public TerrainInfoBase? TerrainInfo { get { return m_terrainInfo; } }

        // try and get an entity from the entity collection in this region
        public virtual bool TryGetEntity(EntityName entName, out IEntity? foundEnt) {
            bool ret = false;
            foundEnt = null;
            IEntityCollection coll;
            if (this.TryGet<IEntityCollection>(out coll)) {
                IEntity ent;
                if (coll.TryGetEntity(entName, out ent)) {
                    foundEnt = ent;
                    ret = true;
                }
            }
            return ret;
        }

        public override void Update(UpdateCodes what) {
            base.Update(what);      // this sends an EntityUpdate for the region
            if (OnRegionUpdated != null) OnRegionUpdated(this, what);
        }

        public override void Dispose() {
            m_terrainInfo = null; // let the garbage collector work
            if (m_regionState != null && m_regionStateChangedCallback != null) {
                State.OnStateChanged -= m_regionStateChangedCallback;
            }
            return;
        }
    }
}
