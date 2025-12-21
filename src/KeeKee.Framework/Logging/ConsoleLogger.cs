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

    public sealed class ConsoleLogger : ILog {

        // there is only one filterlevel used by all instances of the logger class
        private static LogLevel filterLevel = 0;
        public LogLevel FilterLevel { set { filterLevel = value; } get { return filterLevel; } }

        private string moduleName = "";
        public string ModuleName { set { moduleName = value; } get { return moduleName; } }

        public ConsoleLogger() : this("") {
        }

        public ConsoleLogger(string modName) {
            moduleName = modName;
        }

        /// <summary>
        /// return true of a message would be logged with the specified loglevel
        /// </summary>
        /// <param name="logLevel">the loglevel to test if it's enabled now</param>
        /// <returns></returns>
        public bool WouldLog(LogLevel logLevel) {
            return IfLog(logLevel);
        }

        /// <summary>
        /// Internal routine that returns true if the passed logging flag it not filtered
        /// (it will cause some output).
        /// </summary>
        /// <param name="logLevel"></param>
        /// <returns></returns>
        private bool IfLog(LogLevel logLevel) {
            return ((logLevel & filterLevel) != 0);
        }

        /// <summary>
        /// Log the passed message if the loglevel is not filtered out
        /// </summary>
        /// <param name="logLevel">log level of the message</param>
        /// <param name="msg">the message to log</param>
        public void Log(LogLevel logLevel, string msg) {
            if (IfLog(logLevel)) LogIt(logLevel, msg);
        }

        public void Log(LogLevel logLevel, string msg, object p1) {
            if (IfLog(logLevel)) LogIt(logLevel, String.Format(msg, p1));
        }

        public void Log(LogLevel logLevel, string msg, object p1, object p2) {
            if (IfLog(logLevel)) LogIt(logLevel, String.Format(msg, p1, p2));
        }

        public void Log(LogLevel logLevel, string msg, object p1, object p2, object p3) {
            if (IfLog(logLevel)) LogIt(logLevel, String.Format(msg, p1, p2, p3));
        }

        public void Log(LogLevel logLevel, string msg, object p1, object p2, object p3, object p4) {
            if (IfLog(logLevel)) LogIt(logLevel, String.Format(msg, p1, p2, p3, p4));
        }

        private void LogIt(LogLevel logLevel, string msg) {
            StringBuilder buf = new StringBuilder(256);
            buf.Append(DateTime.Now.ToString("yyyyMMddHHmmss"));
            buf.Append(": ");
            buf.Append(KeeKeeBase.ApplicationName);
            buf.Append(": ");
            if (ModuleName.Length != 0) {
                buf.Append(ModuleName);
            }
            buf.Append(": ");
            buf.Append(msg);

            Console.WriteLine(buf.ToString());
        }
    }
}
