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

using OMV = OpenMetaverse;

namespace KeeKee.World {
    /// <summary>
    /// Region context has to do with the space an entity is in. Regions
    /// can overlap. The region defines the space (XYZ) any terrain (heightmap)
    /// and is the basic interface between mapping local coordinates
    /// into the displayed view.
    /// </summary>


    public delegate void RegionRegionStateChangeCallback(RegionContextBase rcontext, RegionStateCode code);
    public delegate void RegionRegionUpdatedCallback(RegionContextBase rcontext, UpdateCodes what);

    // used in TryGetCreateentity calls to create the entity if needed
    public delegate IEntity RegionCreateEntityCallback();

    public interface IRegionContext : IEntity {

        #region Events
        // when a regions state changes
        event RegionRegionStateChangeCallback OnRegionStateChange;

        // when the underlying simulator is changing.
        event RegionRegionUpdatedCallback OnRegionUpdated;

        #endregion Events

        // get the type of the region
        WorldGroupCode WorldGroup { get; }

        // state of teh region
        RegionState State { get; }

        // the size of the region (bounding box)
        OMV.Vector3 Size { get; }

        // the world coordinate of the region's {0,0,0}
        OMV.Vector3d WorldBase { get; }

        // the entities in this region
        IEntityCollection Entities { get; }

        // given an address relative to this region, return a global, world address
        OMV.Vector3d CalculateGlobalPosition(OMV.Vector3 pos);
        OMV.Vector3d CalculateGlobalPosition(float x, float y, float z);

        // information on terrain for this region
        TerrainInfoBase? TerrainInfo { get; }

        /*
        // In  transition requests for getting region entities based on implementation
        // specific info. In this case the LLLP localID. This is part of the conversion
        // of entites being in the world to the entities being in regions.
        bool TryGetEntityLocalID(uint entName, out IEntity ent);
        bool TryGetCreateEntityLocalID(uint localID, out IEntity ent, RegionCreateEntityCallback creater);
         */

    }
}
