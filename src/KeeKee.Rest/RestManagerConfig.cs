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

public class RestManagerConfig {

    public const string subSectionName = "RestManager";

    // Local port used for rest interfaces
    public int Port { get; set; } = 9144;
    // Base URL for rest interfaces
    public string BaseURL { get; set; } = "http://localhost";
    // CSS file for rest display
    public string CSSLocalURL { get; set; } = "/std/KeeKee.css";
    // Directory for static HTML content
    public string UIContentDir { get; set; } =
            Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "./KeeKeeUI");
    // If specified, the subdirectory under StaticContentDir to take files from
    public string APIBase { get; set; } = "/api";
    public string Skin { get; set; } = "Default";
}

