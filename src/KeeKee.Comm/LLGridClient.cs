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

using KeeKee.Framework.Logging;
using OMV = OpenMetaverse;

namespace KeeKee.Comm {

    /// <summary>
    /// Singleton class to hold the creation and instance of GridClient for LLLP communication.
    /// 
    /// TODO: This is a bit of a kludge to get around circular references between CommLLLP and WorldLL.
    /// Need to refactor to eliminate the circular reference and then eliminate this class.
    /// </summary>
    public sealed class LLGridClient {
        private KLogger<LLGridClient> m_Log;
        public LLGridClient(KLogger<LLGridClient> pLog) {
            m_Log = pLog;
        }

        public OMV.GridClient? m_GridClient = null;
        public OMV.GridClient GridClient {
            get {
                if (m_GridClient == null) {
                    m_Log.Log(KLogLevel.Information, "Creating new GridClient");
                    m_GridClient = new OMV.GridClient();
                }
                return m_GridClient;
            }
        }
    }
}
