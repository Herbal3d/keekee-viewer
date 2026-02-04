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

using KeeKee.Contexts;

namespace KeeKee.Renderer {
    /// <summary>
    /// Interface to the persistant storage system which holds the local
    /// copies of rendered objects and textures. Asset storage systems
    /// can be local and remote. The local cache will keep cross session
    /// copies of assets while remote stores will hold larger collections
    /// of assets.
    /// 
    /// Anyone using the persistant store will reference multiple stores
    /// until the asset is found.
    /// 
    /// DEVELOPMENT NOTE: this is unfinished an not linked into any code.
    /// The idea is to abstract the file storage cache system so it can
    /// be replaced someday with a database.
    /// </summary>
    public interface IPersistantStore {
        // return 'true' if we have the entity and are returning a stream to its bits
        bool TryGetEntity(IEntity context, EntityName entName, out Stream bits);

        // return 'true' if this is our storable local cache
        bool isStoreable();

        // store a stream of bits as this entity
        void StoreEntity(IEntity context, EntityName entName, Stream bits);

        /// <summary>
        /// Return 'true' if the entity exists in the cache. This is only good
        /// for checking existance in the local cache. Otherwise the result is
        /// undefined.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="entName"></param>
        /// <returns>'true' if the entity is in the local cache</returns>
        bool ExistsInCache(IEntity context, EntityName entName);


    }
}
