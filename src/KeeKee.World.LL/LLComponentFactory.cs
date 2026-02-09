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

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using KeeKee.Comm;
using KeeKee.Contexts;
using KeeKee.Entity;
using KeeKee.Framework.Logging;

using OMV = OpenMetaverse;

namespace KeeKee.World.LL {

    /// <summary>
    /// The class that manages the creation of components.
    /// This is used to track what types of components are being created
    /// and to allow for future features like component pooling.
    /// </summary>
    public class LLComponentFactory : ComponentFactory {

        public LLComponentFactory(KLogger<LLComponentFactory> pLog,
                                  IServiceProvider pProvider) : base(pLog, pProvider) {
        }

        public override T CreateComponent<T>(params object[] parameters) {
            var cmpt = base.CreateComponent<T>(parameters);
            return cmpt;
        }

        public LLCmptLocation CreateLLCmptLocation(IEntity pContainingEntity) {
            return this.CreateComponent<LLCmptLocation>(pContainingEntity);
        }

        public LLCmptSpecialRender CreateLLCmptSpecialRenderType(IEntity pContainingEntity, IRegionContext pRegionContext) {
            return this.CreateComponent<LLCmptSpecialRender>(pContainingEntity, pRegionContext);
        }
        public LLCmptAvatar CreateLLCmptAvatar(IEntity pContainingEntity) {
            return this.CreateComponent<LLCmptAvatar>(pContainingEntity);
        }
        public LLCmptAgentMovement CreateLLCmptAgentMovement(IEntity pContainingEntity) {
            return this.CreateComponent<LLCmptAgentMovement>(pContainingEntity);
        }

    }
}
