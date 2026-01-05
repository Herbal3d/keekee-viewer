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
using KeeKee.Framework;
using OMV = OpenMetaverse;

namespace KeeKee.World {

    public interface IEntity : IDisposable {
        ulong LGID { get; }
        EntityName Name { get; set; }

        // Contexts for this entity
        IWorld WorldContext { get; }
        IRegionContext RegionContext { get; }
        IAssetContext AssetContext { get; }

        // Returns the entity which implements IEntityCollection which contains this entity
        IEntity? ContainingEntity { get; set; }
        // do what is necessary to set ContainingEntity to null (remove from parent if necessary)
        void DisconnectFromContainer();

        void AddEntityToContainer(IEntity ent);
        void RemoveEntityFromContainer(IEntity ent);

        OMV.Quaternion Heading { get; set; }
        OMV.Vector3 LocalPosition { get; set; }     // position relative to parent (if any)
        OMV.Vector3 RegionPosition { get; }         // position relative to RegionContext
        OMV.Vector3d GlobalPosition { get; }

        // code to check to see if this thing has changed from before
        int LastEntityHashCode { get; set; }
        // Notify the object that some of it state changed
        void Update(UpdateCodes what);

        /// <summary>
        /// An entity is decorated with additional Objects by other subsystems
        /// that either build information about or references to an entity.
        /// These additional objects are kept in a small array of objects for
        /// speed. The index into the array is an integer for the subsystem.
        /// There are predefined codes for the Viewer and Render but other
        /// systems can create a new subsystem index.
        /// </summary>
        Object Addition(int i);
        Object Addition(string s);
        void SetAddition(int i, Object obj);
    }
}
