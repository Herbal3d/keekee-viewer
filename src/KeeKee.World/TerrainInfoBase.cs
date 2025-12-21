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
    protected float[,] m_heightMap;
    public float[,] HeightMap { get { return m_heightMap; } }

    protected int m_heightMapWidth; // X dimension
    public int HeightMapWidth { get { return m_heightMapWidth; } }

    protected int m_heightMapLength; // Y dimension
    public int HeightMapLength { get { return m_heightMapLength; } }

    protected float m_maximumHeight;
    public float MaximumHeight { get { return m_maximumHeight; } }

    protected float m_minimumHeight;
    public float MinimumHeight { get { return m_minimumHeight; } }

    protected int m_terrainPatchStride = 16;
    public int TerrainPatchStride { get { return m_terrainPatchStride; } }

    // X dimension (E/W)
    protected int m_terrainPatchWidth = 256;
    public int TerrainPatchWidth { get { return m_terrainPatchWidth; } }

    // Y dimension (N/S)
    protected int m_terrainPatchLength = 256;
    public int TerrainPatchLength { get { return m_terrainPatchLength; } }

    // height of the water
    public const float NOWATER = -113537;   // here because it can't go in the interface (stupid C#)
    protected float m_waterHeight = NOWATER;
    public float WaterHeight { get { return m_waterHeight; } set { m_waterHeight = value; } }

    // the patch is presumed to be Stride width and length
    public virtual void UpdatePatch(RegionContextBase reg, int x, int y, float[] data) {
        return;
    }

    public TerrainInfoBase (RegionContextBase rcontext, AssetContextBase acontext) 
                    : base(rcontext, acontext) {
    }

    public override void Dispose() {
        throw new NotImplementedException();
    }
}
}
