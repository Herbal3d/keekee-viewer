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

        private OMV.Avatar m_avatar = null;
        public OMV.Avatar Avatar { 
            get { return m_avatar; } 
            set { m_avatar = value; } 
        }

        private OMV.AvatarManager m_avatarManager = null;
        public OMV.AvatarManager AvatarManager {
            get { return m_avatarManager; }
            set { m_avatarManager = value; }
        }

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

        public LLEntityAvatar(AssetContextBase acontext, LLRegionContext rcontext, 
                ulong regionHandle, OMV.Avatar av) : base(rcontext, acontext) {
                // base(acontext, rcontext, regionHandle, av.LocalID, null) { // base for EntityPhysical
            // let people looking at IEntity's get at my avatarness
            RegisterInterface<IEntityAvatar>(this);
            this.Sim = rcontext.Simulator;
            this.RegionHandle = regionHandle;
            this.LocalID = av.LocalID;
            this.Avatar = av;
            this.Name = AvatarEntityNameFromID(acontext, m_avatar.ID);
            LogManager.Log.Log(LogLevel.DCOMMDETAIL, "LLEntityAvatar: create id={0}, lid={1}",
                            av.ID.ToString(), this.LocalID);
        }

        public static EntityName AvatarEntityNameFromID(AssetContextBase acontext, OMV.UUID ID) {
            return new EntityNameLL(acontext, "Avatar/" + ID.ToString());
        }

        #region POSITION
        override public OMV.Quaternion Heading {
            get {
                if (m_avatar != null) {
                    base.Heading = m_avatar.Rotation;
                    return m_avatar.Rotation;
                }
                return base.Heading;
            }
            set {
                base.Heading = value;
                if (m_avatar != null) m_avatar.Rotation = base.Heading;
            }
        }

        override public OMV.Vector3 LocalPosition {
            get {
                if (m_avatar != null) {
                    base.LocalPosition = m_avatar.Position;
                    return m_avatar.Position;
                }
                return base.LocalPosition;
            }
            set {
                base.LocalPosition = value;
                if (m_avatar != null) {
                    m_avatar.Position = base.LocalPosition;
                }
            }
        }
        override public OMV.Vector3d GlobalPosition {
            get {
                // this works in conjunction with the base class to calculate region location
                OMV.Vector3 regionRelative = this.RegionPosition;
                if (m_avatar != null) {
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
                LogManager.Log.Log(LogLevel.DUPDATEDETAIL, "LLEntityAvatar: calling World.UpdateAgent: what={0}", what);
                World.Instance.UpdateAgent(what);
            }
            // do any rendering or whatever for this avatar
        }

        public override void Dispose() {
            return;
        }
    }
}
