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

namespace KeeKee.World {
    public class EntityAvatarBase : EntityBase, IEntityAvatar {

        public EntityAvatarBase(RegionContextBase rcontext, AssetContextBase acontext)
            : base(rcontext, acontext) {
        }

        public virtual string DisplayName {
            get { return this.Name.Name; }
        }
            
        public virtual string ActivityFlags {
            get { return ""; }
        }
            
        override public void Dispose() {
        }
    }
}
