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

namespace KeeKee.World.LL {
    /// <summary>
    /// Lights that fill the world. Used for sun and moon. Individual object 
    /// lighting is done by the entities themselves.
    /// </summary>
    public class LLCmptLight : ICmptLight {
        public string ComponentName { get { return "LLCmptLight"; } }
        public bool Visible { get; set; }

        public OMV.Color4 Color { get; set; }

        public OMV.Vector3 Position { get; set; }
        public OMV.Vector3 Target { get; set; }

        public LLCmptLight() {
            Visible = false;
            Color = new OMV.Color4(1.0f, 1.0f, 1.0f, 1.0f);
            Position = new OMV.Vector3(0, 0, 0);
            Target = new OMV.Vector3(0, 0, 0);
        }

        public void Dispose() {
            return;
        }

    }
}