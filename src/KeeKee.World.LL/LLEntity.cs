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

namespace KeeKee.World.LL {

    public abstract class LLEntity : EntityBase {

        protected IKLogger m_log;
        public OMV.Primitive? Prim { get; set; }

        public const ulong NOREGION = 0xffffffff;
        public ulong RegionHandle { get; set; }

        public OMV.Simulator? Sim { get; set; }

        // an LL localID is a per sim unique handle for the item
        public const uint NOLOCALID = 0xffffffff;
        public uint LocalID { get; set; }

        public LLEntity(IKLogger pLog,
                            IWorld pWorld,
                            IRegionContext pRContext,
                            IAssetContext pAContext)
                        : base(pLog, pWorld, pRContext, pAContext) {
            this.m_log = pLog;

            this.Prim = null;
            this.Sim = null;
            this.RegionHandle = LLEntity.NOREGION;
            this.LocalID = LLEntity.NOLOCALID;
        }

        public override OMV.Quaternion Heading {
            get {
                if (Prim != null) {
                    return this.Prim.Rotation;
                } else {
                    return base.Heading;
                }
            }
            set {
                if (Prim != null) {
                    this.Prim.Rotation = value;
                } else {
                    base.Heading = value;
                }
            }
        }

        public override OMV.Vector3 LocalPosition {
            get {
                if (Prim != null) {
                    base.LocalPosition = Prim.Position;
                    return Prim.Position;
                } else {
                    return base.LocalPosition;
                }
            }
            set {
                base.LocalPosition = value;
                if (Prim != null) {
                    Prim.Position = value;
                }
            }
        }

        public override OMV.Vector3d GlobalPosition {
            get {
                OMV.Vector3 regionRelative = this.RegionPosition;
                if (Prim != null) {
                    return RegionContext.CalculateGlobalPosition(regionRelative);
                } else {
                    return base.GlobalPosition;
                }
            }
        }

        /// <summary>
        /// I am being updated. 
        /// Make sure the parenting is correct before telling the world about any update.
        /// </summary>
        /// <param name="what"></param>
        public override void Update(UpdateCodes what) {
            // TODO: parant management shouldn't be hidden in Update(). Where should it go?
            /*
            // Make sure parenting is correct (we're in our parent's collection)
            try {
                if (this.Prim != null) {    // if no prim, no parent possible
                    uint parentID = this.Prim.ParentID;
                    if (parentID != 0 && this.ContainingEntity == null) {
                        what |= UpdateCodes.ParentID;
                        IEntity parentEntity = null;
                        LLRegionContext rcontext = (LLRegionContext)this.RegionContext;
                        rcontext.TryGetEntityLocalID(parentID, out parentEntity);
                        if (parentEntity != null) {
                            this.ContainingEntity = parentEntity;
                            parentEntity.AddEntityToContainer(this);
                            m_log.Log(KLogLevel.DCOMMDETAIL, "ProcessEntityContainer: adding entity {0} to container {1}",
                                             this.Name, parentEntity.Name);
                        } else {
                            m_log.Log(KLogLevel.DCOMMDETAIL, "Can't assign parent. Entity not found. ent={0}", this.Name);
                        }
                    }
                    if (parentID == 0 && this.ContainingEntity != null) {
                        // the prim has been removed from it's parent
                        what |= UpdateCodes.ParentID;
                        this.DisconnectFromContainer();
                    }
                }
            } catch (Exception e) {
                m_log.Log(KLogLevel.DBADERROR, "FAILED ProcessEntityContainer: " + e);
            }
            */

            // tell the world about our updating
            base.Update(what);
        }
    }
}
