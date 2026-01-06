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
    public class LLCmptLocation : IEntityComponent, ICmptLocation {

        private IKLogger m_log;

        public string ComponentName { get { return "LLCmptLocation"; } }
        private OMV.GridClient m_client;
        private IEntity m_containingEntity;

        public LLCmptLocation(IKLogger pLog,
                            OMV.GridClient theClient,
                            IEntity pContainingEntity) {
            m_log = pLog;
            m_client = theClient;
            m_containingEntity = pContainingEntity;
        }

        private bool m_haveLocalHeading = false;
        private OMV.Quaternion m_heading;
        public OMV.Quaternion Heading {
            get {
                // kludge to allow the local agent to be different for dead reconning
                if (m_haveLocalHeading) {
                    return m_heading;
                }
                return m_client.Self.SimRotation;
            }
            set {
                m_heading = value;
                m_haveLocalHeading = true;
            }
        }

        private OMV.Vector3 m_localPosition;
        public OMV.Vector3 LocalPosition {
            get {
                if (m_containingEntity != null) {
                    m_localPosition = m_client.Self.SimPosition;
                    return m_client.Self.SimPosition;
                }
                return m_localPosition;
            }
            set {
                m_localPosition = value;
            }
        }

        public OMV.Vector3 RegionPosition {
            get {
                return this.LocalPosition;
            }
        }

        public OMV.Vector3d GlobalPosition {
            get {
                return m_client.Self.GlobalPosition;
                // return AssociatedAvatar.RegionContext.CalculateGlobalPosition(RelativePosition);
            }
        }
        
        public void Dispose() {
            // nothing to do
        }
    }
}

