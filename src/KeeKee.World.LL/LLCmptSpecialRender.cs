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
    public class LLCmptSpecialRenderType : IEntityComponent, ISpecialRender {
        public IEntity ContainingEntity { get; private set; }

        private IKLogger m_log;

        private OMV.GridClient m_client;
        private IRegionContext m_regionContext;

        public LLCmptSpecialRenderType(IKLogger pLog,
                                IEntity pContainingEntity,
                                OMV.GridClient pClient,
                                IRegionContext pRegionContext) {
            m_log = pLog;
            ContainingEntity = pContainingEntity;
            m_client = pClient;
            m_regionContext = pRegionContext;
        }
        public SpecialRenderTypes Type { get; set; }

        public OMV.PCode FoliageType { get; set; }
        public OMV.Tree TreeType { get; set; }
        public OMV.Grass GrassType { get; set; }
        public void Dispose() {
            return;
        }

    }
}

