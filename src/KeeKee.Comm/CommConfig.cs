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

using LibreMetaverse;

namespace KeeKee.Comm {
    public class CommConfig {
        public static string subSectionName { get; set; } = "Comm";

        public AssetConfig Assets { get; set; } = new AssetConfig();

        // Whether Comm should hold objects if the parent doesn't exist
        public bool ShouldHoldChildren { get; set; } = true;
        // Wether to connect to multiple sims
        public bool MultipleSims { get; set; } = false;
        // Milliseconds between movement messages sent to server
        public int MovementUpdateInterval { get; set; } = 100;
        public ConnectionConfig Connection { get; set; } = new ConnectionConfig();
        public WorldConfig World { get; set; } = new WorldConfig();
        public LLAgentConfig LLAgent { get; set; } = new LLAgentConfig();

    }

    // ============================================================
    public class AssetConfig {
        // Filesystem location to build the texture cache
        public string? CacheDir { get; set; }
        // Whether to use the caps asset system if available
        public bool EnableCaps { get; set; } = true;
        // "Maximum number of outstanding textures requests
        public int MaxTextureRequests { get; set; } = 10;
        // Directory for resources used by libopenmetaverse (mostly for appearance)
        public string OMVResources { get; set; } = "./KeeKeeResources/openmetaverse_data";
        // Filename of texture to display when we can't get the real texture
        public string NoTextureFilename { get; set; } = "./KeeKeeResources/NoTexture.png";
        // Filename of texture to display when we can't get the real sculpty texture
        public string NoSculptyFilename { get; set; } = "./KeeKeeResources/NoSculpty.png";
        // whether to convert incoming JPEG2000 files to PNG files in the cache
        public bool ConvertPNG { get; set; } = true;
    }

    public class LLAgentConfig {
        // Distance in meters to consider "close enough" for sitting on an object
        public float SitCloseEnough { get; set; } = 1.5f;
        // Whether to move avatar when user types (otherwise wait for server round trip)");
        public bool PreMoveAvatar { get; set; } = true;
        // Degrees to rotate avatar when user turns (float)
        public float PreMoveRotFudge { get; set; } = 3.0f;
        // Meters to move avatar when moves forward when flying (float)
        public float PreMoveFlyFudge { get; set; } = 2.5f;
        // Meters to move avatar when moves forward when running (float)
        public float PreMoveRunFudge { get; set; } = 1.5f;
        //"Meters to move avatar when moves forward when walking (float)
        public float PreMoveFudge { get; set; } = 0.4f;
    }
    public class ConnectionConfig {
        // Application name sent when logging in
        public string ApplicationName { get; set; } = "KeeKee Viewer";
        // Version string sent when logging in
        public string Version { get; set; } = "0.1.0";
    }

    public class WorldConfig {
        // Maximum number of objects to request in a single GetObjects call
        public int MaxObjectsPerRequest { get; set; } = 100;
        // Number of retries for object requests
        public int ObjectRequestRetries { get; set; } = 3;
        // Milliseconds to wait before retrying object request
        public int ObjectRequestRetryDelayMS { get; set; } = 500;

        public LLAgentConfig LLAgent { get; set; } = new LLAgentConfig();

    }
}
