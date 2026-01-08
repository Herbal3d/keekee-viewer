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

using KeeKee;
using KeeKee.Framework.Logging;
using KeeKee.World;

using OMV = OpenMetaverse;

namespace KeeKee.World.LL {
    public class LLCmptAttachment : IEntityComponent {
        private IKLogger m_log;

        public IEntity ContainingEntity { get; private set; }

        private OMV.GridClient m_client;
        public string AttachmentID;
        public OMV.AttachmentPoint AttachmentPoint;
        public LLCmptAttachment(IKLogger pLog,
                        IEntity pContainingEntity,
                        OMV.GridClient pClient
                        ) {
            m_log = pLog;
            ContainingEntity = pContainingEntity;
            m_client = pClient;
        }

        public void Dispose() {
            return;
        }
    }
}
