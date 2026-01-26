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
    public class LLCmptAvatar : ICmptAvatar, IEntityComponent {

        private IKLogger m_log;
        public IEntity ContainingEntity { get; private set; }
        private OMV.GridClient m_client;

        public OMV.UUID? AvatarUUID { get; set; }

        public string DisplayName { get; set; } = "";
        public string ActivityFlags { get; set; } = ""; // e.g. "Sitting, AFK"

        public LLCmptAvatar(IKLogger pLog,
                                IEntity pContainingEntity,
                                OMV.GridClient pClient
                                ) {
            m_log = pLog;
            m_client = pClient;
            ContainingEntity = pContainingEntity;
        }

        public string FullName {
            get {
                if (AvatarUUID.HasValue) {
                    var avName = m_client.Self.Name;
                    if (avName != null) {
                        return avName.ToString();
                    } else {
                        return $"FullName: Unknown Avatar {AvatarUUID.ToString()}";
                    }
                } else {
                    return "No Avatar UUID";
                }
            }
        }
        public string First {
            get {
                if (AvatarUUID.HasValue) {
                    var avName = m_client.Self.FirstName;
                    if (avName != null) {
                        return avName.ToString();
                    } else {
                        return $"First: Unknown Avatar {AvatarUUID.ToString()}";
                    }
                } else {
                    return "No Avatar UUID";
                }
            }
        }
        public string Last {
            get {
                if (AvatarUUID.HasValue) {
                    var avName = m_client.Self.LastName;
                    if (avName != null) {
                        return avName.ToString();
                    } else {
                        return $"Last: Unknown Avatar {AvatarUUID.ToString()}";
                    }
                } else {
                    return "No Avatar UUID";
                }
            }
        }

        public void Dispose() {
            return;
        }
    }
}

