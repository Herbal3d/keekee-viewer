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

namespace KeeKee.Config {
    public class GridConfig {
        public static string subSectionName { get; set; } = "Grids";

        public class GridDefinition {
            public string LoginURI { get; set; } = "https://login.secondlife.com/cgi-bin/login.cgi";
            public string LoginPage { get; set; } = "https://login.secondlife.com/cgi-bin/login.cgi";
            public string HelperURI { get; set; } = "http://grid.secondlife.com/";
            public string WebSite { get; set; } = "http://secondlife.com/helpers/";
            public string SupportURL { get; set; } = "http://secondlife.com/";
            public string AccountURL { get; set; } = "http://secondlife.com/";
            public string PasswordURL { get; set; } = "http://secondlife.com/";
            public string Platform { get; set; } = "OpenSimulator";
        }

        public Dictionary<string, GridDefinition> Grids { get; set; } = new Dictionary<string, GridDefinition>();
    }
}

