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
    public class WorldConfig
    {
        public static string subSectionName { get; set; } = "World";

        // Maximum number of objects to request in a single GetObjects call
        public int MaxObjectsPerRequest { get; set; } = 100;
        // Number of retries for object requests
        public int ObjectRequestRetries { get; set; } = 3;
        // Milliseconds to wait before retrying object request
        public int ObjectRequestRetryDelayMS { get; set; } = 500;

        public LLAgentConfig LLAgent { get; set; } = new LLAgentConfig();

    }
}

