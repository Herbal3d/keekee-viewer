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

namespace KeeKee.Config
{

    public class LLAgentConfig
    {
        public static string subSectionName { get; set; } = "LLAgent";

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
}
