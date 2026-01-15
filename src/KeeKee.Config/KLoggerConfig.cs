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
    public class KLoggerConfig {

        // Configuration subsection name
        public const string subSectionName = "KLogger";

        public string LogLevel { get; set; } = "Information";

        // Detailed logging from the REST manager
        public bool RestDetail { get; set; } = false;
        // Detailed logging from the Work Queue manager
        public bool WorkQueueDetail { get; set; } = false;
        public bool UIDetail { get; set; } = false;
        // Detailed initialization logging
        public bool DINIT { get; set; } = false;
        public bool DINITDETAIL { get; set; } = false;
        public bool DCOMM { get; set; } = false;
        public bool DCOMMDETAIL { get; set; } = false;
        public bool DWORLD { get; set; } = false;
        public bool DWORLDDETAIL { get; set; } = false;
        public bool DUPDATE { get; set; } = false;
        public bool DUPDATEDETAIL { get; set; } = false;
        public bool DTEXTURE { get; set; } = false;
        public bool DTEXTUREDETAIL { get; set; } = false;
        public bool DRENDER { get; set; } = false;
        public bool DRENDERDETAIL { get; set; } = false;
        public bool DVIEW { get; set; } = false;
        public bool DVIEWDETAIL { get; set; } = false;
    }

}
