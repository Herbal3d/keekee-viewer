
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

using Microsoft.Extensions.Logging;

namespace KeeKee.Framework.Logging {

    public enum KLogLevel {
        Trace,
        Debug,
        Information,
        Warning,
        Error,
        Critical,
        None,
        DBADERROR,
        RestDetail,
        WorkQueueDetail,
        UIDetail,
        DINIT,
        DINITDETAIL,
        DCOMM,
        DCOMMDETAIL,
        DWORLD,
        DWORLDDETAIL,
        DUPDATE,
        DUPDATEDETAIL,
        DTEXTURE,
        DTEXTUREDETAIL
    }
    public interface IKLogger : ILogger {
        // Log with our KLogLevel
        void Log(KLogLevel level, string message, params object[] args);
        public void LogInfo(string message, params object[] args);

        public void LogDebug(string message, params object[] args);

        public void LogError(string message, params object[] args);
    }
}
