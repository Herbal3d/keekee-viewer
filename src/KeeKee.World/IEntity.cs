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

using KeeKee.Framework;
using KeeKee.Framework.Logging;
using OMV = OpenMetaverse;

namespace KeeKee.World {

    public interface IEntity : IDisposable {
        // The logger for this entity
        IKLogger EntityLogger { get; }

        // The local unique ID for this entity
        ulong LGID { get; }
        EntityName Name { get; set; }

        // Contexts for this entity
        IWorld WorldContext { get; }
        IRegionContext RegionContext { get; }
        IAssetContext AssetContext { get; }

        // Returns the entity which implements IEntityCollection which contains this entity
        IEntity? ContainingEntity { get; set; }

        // code to check to see if this thing has changed from before
        BHash LastEntityHashCode { get; set; }

        // Notify the object that some of it state changed
        void Update(UpdateCodes what);

        // Component management
        T Cmpt<T>() where T : class, IEntityComponent;
        void AddComponent<T>(T component) where T : class, IEntityComponent;
        bool HasComponent<T>() where T : class, IEntityComponent;
        bool HasComponent<T>(out T? component) where T : class, IEntityComponent;
    }
}
