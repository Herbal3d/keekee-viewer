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

namespace KeeKee.Contexts {
    public interface ITerrainInfo {
        // generic terrain description
        float[,] HeightMap { get; }
        int HeightMapWidth { get; }  // X dimension
        int HeightMapLength { get; } // Y dimension
        float MaximumHeight { get; }
        float MinimumHeight { get; }
        float WaterHeight { get; set; }

        // update the height info for a patch cornered at x, y
        void UpdatePatch(IRegionContext reg, int x, int y, float[] data);

        // terrain is a problem. Here are several constants used in its represtantation
        // TODO: better definition of terrain types and characteristics
        // make a general terrain heightmap that is presented by the world
        int TerrainPatchStride { get; }
        int TerrainPatchWidth { get; }      // X dimension (E/W)
        int TerrainPatchLength { get; }     // Y dimension (N/S)

    }
}
