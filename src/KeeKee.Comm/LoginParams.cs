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

using OMVSD = OpenMetaverse.StructuredData;

namespace KeeKee.Comm {

    /// <summary>
    /// Block of information needed to login to a grid.
    /// General version not tied to any particular communication library.
    /// </summary>
    public class LoginParams {
        public virtual string? UserName { get; set; }
        public virtual string? Password { get; set; }
        public virtual string? AuthURL { get; set; }
        public virtual string? FirstName { get; set; }
        public virtual string? LastName { get; set; }
        public virtual string? StartLocation { get; set; }
        public virtual string? HomeURL { get; set; }
        public virtual string? Grid { get; set; }

        public void FromOSD(OMVSD.OSD osd) {
            if (osd is OMVSD.OSDMap map) {
                UserName = map.ContainsKey("UserName") ? map["UserName"].AsString() : null;
                Password = map.ContainsKey("Password") ? map["Password"].AsString() : null;
                AuthURL = map.ContainsKey("AuthURL") ? map["AuthURL"].AsString() : null;
                FirstName = map.ContainsKey("FirstName") ? map["FirstName"].AsString() : null;
                LastName = map.ContainsKey("LastName") ? map["LastName"].AsString() : null;
                StartLocation = map.ContainsKey("StartLocation") ? map["StartLocation"].AsString() : null;
                HomeURL = map.ContainsKey("HomeURL") ? map["HomeURL"].AsString() : null;
                Grid = map.ContainsKey("Grid") ? map["Grid"].AsString() : null;
            }
        }
    }
}
