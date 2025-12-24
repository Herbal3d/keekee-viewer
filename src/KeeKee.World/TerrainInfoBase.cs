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
    public class TerrainInfoBase : EntityBase, ITerrainInfo {
        public float[,] HeightMap { get; protected set; } = new float[256, 256];

        public int HeightMapWidth { get; protected set; } = 256;

        public int HeightMapLength { get; protected set; } = 256;

        public float MaximumHeight { get; protected set; } = 4096.0f;
        public float MinimumHeight { get; protected set; } = -4096.0f;

        public int TerrainPatchStride { get; protected set; } = 16;

        // X dimension (E/W)
        public int TerrainPatchWidth { get; protected set; } = 256;

        // Y dimension (N/S)
        public int TerrainPatchLength { get; protected set; } = 256;

        // height of the water
        public const float NOWATER = -113537;   // here because it can't go in the interface (stupid C#)
        public float WaterHeight { get; set; } = NOWATER;

        // the patch is presumed to be Stride width and length
        public virtual void UpdatePatch(RegionContextBase reg, int x, int y, float[] data) {
            return;
        }

        public TerrainInfoBase(RegionContextBase rcontext, AssetContextBase acontext)
                        : base(rcontext, acontext) {
        }

        public override void Dispose() {
            throw new NotImplementedException();
        }
    }
}
