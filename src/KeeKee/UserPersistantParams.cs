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

namespace KeeKee {

    // The viewer can remember previous accounts used
    public class UPPAccounts {
        public required string Username;
        public required string Grid;
        public required string FirstName;
        public required string LastName;
        public string? StartLocation;
    }

    public enum UPPFileVersion {
        V1 = 1,
    }

    /// <summary>
    /// Collection of user parameters that are saved between runs.
    /// This allows the user toe set them once and have them persist.
    /// </summary>
    public class UserPersistantParams {
        public UPPFileVersion FileVersion { get; set; } = UPPFileVersion.V1;
        public List<UPPAccounts> SavedAccounts { get; set; } = new List<UPPAccounts>();
        public string LastUsername { get; set; } = "YourUserName";
        public bool ShouldSaveUsername { get; set; } = false;
        public bool ShouldSavePassword { get; set; } = false;

        public static void WriteUserPersistantParams() {
            // Placeholder for future implementation
        }

        public static UserPersistantParams ReadUserPersistantParams() {
            // Placeholder for future implementation
            return new UserPersistantParams();
        }
    }

}
