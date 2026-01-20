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

using System.Reflection;

namespace KeeKee.Config {

    public class KeeKeeConfig {
        public const string subSectionName = "KeeKeeViewer";

        public string AppTitle { get; set; } = "KeeKee Viewer";
        public string AppName { get; set; } = "KeeKeeViewer";

        // Static properties to access build-time version info
        // Refer to Directory.Build.props for setting these values
        public static string GitHash => GetAssemblyMetadata("GitHash") ?? "unknown";
        public static string BuildTimestamp => GetAssemblyMetadata("BuildTimestamp") ?? "unknown";
        public static string InformationalVersion => GetAssemblyMetadata("InformationalVersion") ?? "unknown";
        public static string FullVersion =>
            Assembly.GetEntryAssembly()?
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion ?? "0.0.0+unknown";

        private static string? GetAssemblyMetadata(string key) {
            return Assembly.GetEntryAssembly()?
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .FirstOrDefault(a => a.Key == key)?
                .Value;
        }
    }
}

