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
using OMV = OpenMetaverse;

namespace KeeKee.World.LL {
    public class LLEntityPhysical : LLEntityBase {

        public LLEntityPhysical(AssetContextBase acontext, LLRegionContext rcontext, 
                ulong regionHandle, uint localID, OMV.Primitive prim) : base(rcontext, acontext) {
            this.Sim = rcontext.Simulator;
            this.RegionHandle = regionHandle;
            this.LocalID = localID;
            this.Prim = prim;
            this.Name = new EntityNameLL(acontext, m_prim.ID.ToString());
        }

        public override void Dispose() {
            throw new NotImplementedException();
        }
    }
}
