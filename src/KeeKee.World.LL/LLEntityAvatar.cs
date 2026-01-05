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
using KeeKee;
using KeeKee.Framework.Logging;
using KeeKee.World;
using OMV = OpenMetaverse;

namespace KeeKee.World.LL {
    public sealed class LLEntityAvatar : LLEntityBase, IEntityAvatar {

        public OMV.Avatar Avatar { get; set; }

        public OMV.AvatarManager? AvatarManager { get; set; }

        public string DisplayName {
            get {
                if (this.Avatar != null) {
                    return this.Avatar.FirstName + " " + this.Avatar.LastName;
                }
                return this.Name.Name;
            }
        }

        public string ActivityFlags {
            get {
                string ret = "";
                if (this.Avatar != null) {
                    if ((this.Avatar.Flags & OMV.PrimFlags.Flying) != 0) {
                        ret += "F";
                    }
                }
                return ret;
            }
        }

        public LLEntityAvatar(KLogger<LLEntityAvatar> pLog,
                            IAssetContext pAContext,
                            LLRegionContext pRContext,
                            ulong regionHandle,
                            OMV.Avatar av)
                        : base(pLog, pRContext, pAContext) {

            // let people looking at IEntity's get at my avatarness
            this.Sim = pRContext.Simulator;
            this.RegionHandle = regionHandle;
            this.LocalID = av.LocalID;
            this.Avatar = av;
            this.Name = AvatarEntityNameFromID(pAContext, Avatar.ID);

            m_log.Log(KLogLevel.DCOMMDETAIL, "LLEntityAvatar: create id={0}, lid={1}",
                            av.ID.ToString(), this.LocalID);
        }

        public static EntityName AvatarEntityNameFromID(AssetContextBase pAContext, OMV.UUID ID) {
            return new EntityNameLL(pAContext, "Avatar/" + ID.ToString());
        }

        #region POSITION
        override public OMV.Quaternion Heading {
            get {
                if (Avatar != null) {
                    base.Heading = Avatar.Rotation;
                    return Avatar.Rotation;
                }
                return base.Heading;
            }
            set {
                base.Heading = value;
                if (Avatar != null) Avatar.Rotation = base.Heading;
            }
        }

        override public OMV.Vector3 LocalPosition {
            get {
                if (Avatar != null) {
                    base.LocalPosition = Avatar.Position;
                    return Avatar.Position;
                }
                return base.LocalPosition;
            }
            set {
                base.LocalPosition = value;
                if (Avatar != null) {
                    Avatar.Position = base.LocalPosition;
                }
            }
        }
        override public OMV.Vector3d GlobalPosition {
            get {
                // this works in conjunction with the base class to calculate region location
                OMV.Vector3 regionRelative = this.RegionPosition;
                if (Avatar != null) {
                    return m_regionContext.CalculateGlobalPosition(regionRelative);
                }
                return base.GlobalPosition;
            }
        }
        #endregion POSITION

        public override void Update(UpdateCodes what) {
            // if we are the agent in the world, also update the agent
            base.Update(what);
            if (World.Instance.Agent != null && this == World.Instance.Agent.AssociatedAvatar) {
                m_log.Log(KLogLevel.DUPDATEDETAIL, "LLEntityAvatar: calling World.UpdateAgent: what={0}", what);
                World.Instance.UpdateAgent(what);
            }
            // do any rendering or whatever for this avatar
        }

        public override void Dispose() {
            return;
        }
    }
}
