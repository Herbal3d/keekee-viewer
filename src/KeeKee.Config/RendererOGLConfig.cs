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

namespace KeeKee.Config {

    public class RendererOGLConfig {
        public static string subSectionName { get; set; } = "RendererOGL";

        // camera far clip
        public float CamaraFar { get; set; } = 2048.0f;
        public string GlobalAmbient { get; set; } = "<0.4,0.4,0.4>";
        public string SunAmbient { get; set; } = "<0.4,0.4,0.4>";
        public string SunSpecular { get; set; } = "<0.8,0.8,0.8>";
        public string SunDiffuse { get; set; } = "<1.0,1.0,1.0>";
        public string MoonAmbient { get; set; } = "<0.2,0.2,0.2>";
        public string MoonSpecular { get; set; } = "<0.5,0.5,0.5>";
        public string MoonDiffuse { get; set; } = "<0.5,0.5,0.5>";
        public string AvatarColor { get; set; } = "<0.4,0.4,0.4>";    // Color of avatar shape
        public float AvatarTransparancy { get; set; } = 0.6f;          // Transparacy of avatar shape
    }
}
