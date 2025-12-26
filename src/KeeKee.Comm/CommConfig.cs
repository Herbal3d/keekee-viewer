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

namespace KeeKee.Comm {
    public class CommConfig {
            public string ProviderName { get; set; } = "LL";
            public string LoginURI { get; set; } = "https://login.agni.lindenlab.com/cgi-bin/login.cgi";
            public string UserName { get; set; } = "YourUserName";
            public string Password { get; set; } = "YourPassword";
            public string FirstName { get; set; } = "First";
            public string LastName { get; set; } = "Last";
            public string StartLocation { get; set; } = "My Home";
            public bool AutoConnect { get; set; } = true;
        }

}
