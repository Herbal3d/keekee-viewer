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

using System;
using System.Collections.Generic;
using System.Text;

namespace KeeKee.Framework.Logging {
    public class LogManager {
        private static ILog m_log = null;
        public static ILog Log { get { return m_log; } set { m_log = value; } }

        private static LogLevel m_currentLogLevel;
        public static LogLevel CurrentLogLevel {
            get { return m_currentLogLevel; }
            set { m_currentLogLevel = value; }
        }

        static LogManager() {
            // a global logger that makes sure logging is set up early on
            m_log = LogManager.GetLogger(KeeKeeBase.ApplicationName);
            // initially we assume all but this will be updated in Main
            CurrentLogLevel = LogLevel.DALL;
        }

        static public ILog GetLogger() {
            ILog newLogger = new Log4NetLogger();
            return newLogger;
            // return new ConsoleLogger();
        }

        static public ILog GetLogger(string modName) {
            return new Log4NetLogger(modName);
            // return new ConsoleLogger(modName);
        }
    }
}
