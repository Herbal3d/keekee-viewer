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

using KeeKee.Framework.WorkQueue;

using OMV = OpenMetaverse;

namespace KeeKee.World {

    public enum AssetType {
        Texture,
        SculptieTexture,
        BakedTexture
    }

    /// <summary>
    /// Base class for the asset context associated with an entity. This provides
    /// functions for accessing the contents of the entity (prim info, textures, ...).
    /// 
    /// A particular type of asset implmentation extends this with the operations
    /// that actually find and fetch the data. The information goes into the caching
    /// system for access by the renderer and other subsystems.
    /// </summary>
    /// 
    // When a requested download is finished, you can be called with the ID of the
    // completed asset and the entityName of ??

    public interface IAssetContext {

        public class WaitingInfo : IComparable<WaitingInfo> {
            public OMV.UUID worldID;
            public string filename;
            public AssetType type;
            public TaskCompletionSource<AssetLoadInfo>? tcs;
            public WaitingInfo(OMV.UUID wid) {
                worldID = wid;
            }
            public int CompareTo(WaitingInfo? other) {
                return other == null ? 1 : worldID.CompareTo(other.worldID);
            }
        }

        public class AssetLoadInfo {
            public OMV.TextureRequestState DownloadState;
            public OMV.Assets.AssetTexture AssetData;
            public EntityName entityName;
            public AssetType type = AssetType.Texture;
            public bool HasTransparancy = false;
            public AssetLoadInfo(EntityName ename, AssetType typ,
                                    OMV.TextureRequestState state,
                                    OMV.Assets.AssetTexture assetData) {
                entityName = ename;
                type = typ;
                DownloadState = state;
                AssetData = assetData;
            }
        }

        public string Name { get; set; }

        public string CacheDirBase { get; set; }

        // used to lock access to the filesystem so the threads and instances of this don't get too tangled
        public static readonly object FileSystemAccessLock = new object();

        /// <summary>
        /// Given a context and a world specific identifier, return the filename
        /// (without the CacheDirBase included) of the texture file. This may start
        /// the loading of the texture so the texture file will be updated and
        /// call to the OnDownload* events will show it's progress.
        /// </summary>
        /// <param name="textureEntityName">the entity name of this texture</param>
        public Task<AssetLoadInfo> DoTextureLoad(EntityName pTextureEntityName, AssetType pType);

        /// <summary>
        /// the caller didn't know who the owner of the texture was. We take apart the entity
        /// name to try and find who it belongs to. This is static since we are using the static
        /// asset context structures. When we find the real asset context of the texture, we
        /// call that instance.
        /// </summary>
        /// <param name="textureEntityName"></param>
        /// <param name="finished"></param>
        /// <returns></returns>
        public Task<AssetLoadInfo> RequestTextureLoad(EntityName pTextureEntityName, AssetType pType);

        /// <summary>
        /// based only on the name of the texture entity, have te asset context decide if it
        /// is the owner of this texture.
        /// </summary>
        /// <param name="textureEntityName"></param>
        /// <returns></returns>
        public bool isTextureOwner(EntityName textureEntityName);

        public void Dispose();
    }
}

