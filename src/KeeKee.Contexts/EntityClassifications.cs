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

namespace KeeKee.Contexts {
    /// <summary>
    /// Entities fall into various groupings that effect how they are handled.
    /// This enumeration defines those groupings.
    public enum EntityClassifications {
        Unknown = 0,
        WorldContext = 1,
        RegionContext = 2,
        AssetContext = 3,
        UserContext = 4,
        AvatarEntity = 10,
        ObjectEntity = 11,
        TerrainEntity = 12,
        PrimitiveEntity = 13,
        AttachmentEntity = 14,
        InventoryItemEntity = 20,
        InventoryFolderEntity = 21,
    }
}
