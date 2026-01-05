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
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using KeeKee.Framework.Logging;
using KeeKee.World;
using OMV = OpenMetaverse;

namespace KeeKee.World.LL {
    // class to hold the LLLP specific name conversion routines
    public class EntityNameLL : EntityName {

        // being created with just a resource name. We extract the parts
        public EntityNameLL(string name) : base(name) {
        }

        public EntityNameLL(EntityName ent)
                : base(ent.Name) {
        }

        public EntityNameLL(IEntity entityContext, string name)
                : base(entityContext.AssetContext, name) {
        }

        public EntityNameLL(AssetContextBase acontext, string name)
                : base(acontext, name) {
        }

        public static EntityNameLL ConvertTextureWorldIDToEntityName(AssetContextBase context, OMV.UUID textureWorldID) {
            return ConvertTextureWorldIDToEntityName(context, textureWorldID.ToString());
        }

        public static EntityNameLL ConvertTextureWorldIDToEntityName(AssetContextBase context, string textureWorldID) {
            return new EntityNameLL(context, textureWorldID);
        }

        // Return the cache filename for this entity. This is not based in the cache directory.
        // At the moment, closely tied to the Ogre resource storage structure
        public override string CacheFilename {
            get {
                string entReplace = Regex.Replace(EntityPart, EntityNameMatch, CachedNameReplace);
                // if the replacement didn't happen entReplace == entName
                string newName = base.CombineEntityName(HeaderPart, HostPart, entReplace);
                // LogManager.Log.Log(LogLevel.DRENDERDETAIL, "ConvertTextureEntityNameToCacheFilename: " + entName.ToString() + " => " + newName);

                // if windows, fix all the entity separators so they become directory separators
                if (Path.DirectorySeparatorChar != '/') {
                    newName.Replace('/', Path.DirectorySeparatorChar);
                }
                return newName;
            }
        }

        // This class has a little more specific knowlege of how the complete entity name
        // can be converted into its parts or override the default routines.
        private const string HostPartMatch = @"^(.*)/........-....-....-....-.*$";
        private const string HostPartReplace = @"$1";
        // the host part is embedded in the name somewhere. See if we can find it.
        public override string ExtractHostPartFromEntityName() {
            return Regex.Replace(this.Name, HostPartMatch, HostPartReplace);
        }

        private const string UUIDMatch = @"^.*/(........-....-....-....-............).*$";
        private const string UUIDReplace = @"$1";
        // LL entities have a UUID in them of the real name of the entity
        public override string ExtractEntityFromEntityName() {
            return Regex.Replace(this.Name, UUIDMatch, UUIDReplace);
        }

    }
}
