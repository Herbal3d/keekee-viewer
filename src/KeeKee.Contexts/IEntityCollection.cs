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

    public delegate void EntityNewCallback(IEntity ent);
    public delegate void EntityUpdateCallback(IEntity ent, UpdateCodes what);
    public delegate void EntityRemovedCallback(IEntity ent);

    // used in TryGetCreateentity calls to create the entity if needed
    public delegate IEntity CreateEntityCallback();

    /// <summary>
    /// A collection of entities. Any entity that is a 'parent' of a set
    /// of other entities has one of these collections. The entities
    /// in teh collection are the 'children' of the entity. Entity collections
    /// are also kept by regions to hold all the top level entities
    /// in that region.
    /// </summary>
    public interface IEntityCollection : IDisposable {
        // when new items are added to the world
        event EntityNewCallback? OnEntityNew;
        event EntityUpdateCallback? OnEntityUpdate;
        event EntityRemovedCallback? OnEntityRemoved;

        int Count { get; }

        void AddEntity(IEntity entity);

        void UpdateEntity(IEntity entity, UpdateCodes detail);

        void RemoveEntity(IEntity entity);

        /// <summary>
        /// Find an entity based on a LGID.
        /// </summary>
        /// <param name="lgid"></param>
        /// <param name="ent"></param>
        /// <returns>'true' if the entity was found</returns>
        bool TryGetEntity(ulong lgid, out IEntity ent);

        /// <summary>
        /// Find an entity based on its name
        /// </summary>
        /// <param name="entName"></param>
        /// <param name="ent"></param>
        /// <returns>'true' if the entity was found</returns>
        bool TryGetEntity(string entName, out IEntity ent);

        /// <summary>
        /// Find an entity based on its name
        /// </summary>
        /// <param name="entName"></param>
        /// <param name="ent"></param>
        /// <returns>'true' if the entity was found</returns>
        bool TryGetEntity(EntityName entName, out IEntity ent);

        /// <summary>
        /// Try to get an entity and create it if it doesn't exist.  If the
        /// entity is not found, the 'createIt' delegate is called to create
        /// the entity. Thus this routine always returns an entity.
        /// </summary>
        /// <param name="entName">name of entity to search for</param>
        /// <param name="ent">found entity</param>
        /// <param name="createIt">delegate called to create the entity if it doesn't exist</param>
        /// <returns>true if we created a new entry</returns>
        bool TryGetCreateEntity(EntityName entName, out IEntity? ent, CreateEntityCallback createIt);

        IEntity FindEntity(Predicate<IEntity> pred);

        void ForEach(Action<IEntity> act);
    }
}
