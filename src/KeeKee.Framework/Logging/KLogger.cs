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
using Microsoft.Extensions.Options;

namespace KeeKee.Framework.Logging {
    /// <summary>
    /// Wrapper around ILogger to provide KeeKee-specific logging features.
    /// Implements IKLogger for dependency injection.
    /// </summary>
    /// <typeparam name="T">The type associated with the logger (for categorization).</typeparam>
    public class KLogger<T> : IKLogger<T> {
        private readonly ILogger<T> _innerLogger;
        private readonly string _categoryName = typeof(T).FullName ?? nameof(T);
        private readonly IOptions<KLoggerConfig> _options;

        // This log level is used for detail logging
        private const LogLevel LLForDetail = LogLevel.Information;

        public KLogger(ILogger<T> pLogger, IOptions<KLoggerConfig> pOptions) {
            _innerLogger = pLogger ?? throw new ArgumentNullException(nameof(pLogger));
            _options = pOptions ?? throw new ArgumentNullException(nameof(pOptions));
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull {
            return _innerLogger.BeginScope(state);
        }

        public bool IsEnabled(LogLevel logLevel) {
            return _innerLogger.IsEnabled(logLevel);
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
            where TState : notnull {
            _innerLogger.Log(logLevel, eventId, state, exception, formatter);
        }

        public void Log(KLogLevel level, string message, params object[] args) {
            switch (level) {
                case KLogLevel.Trace:
                    _innerLogger.Log(LogLevel.Trace, message, args);
                    break;
                case KLogLevel.Debug:
                    _innerLogger.Log(LogLevel.Debug, message, args);
                    break;
                case KLogLevel.Information:
                    _innerLogger.Log(LogLevel.Information, message, args);
                    break;
                case KLogLevel.Warning:
                    _innerLogger.Log(LogLevel.Warning, message, args);
                    break;
                case KLogLevel.Error:
                    _innerLogger.Log(LogLevel.Error, message, args);
                    break;
                case KLogLevel.Critical:
                    _innerLogger.Log(LogLevel.Critical, message, args);
                    break;
                case KLogLevel.UIDetail:
                    if (_options.Value.UIDetail)
                        _innerLogger.Log(LLForDetail, message, args);
                    break;
                case KLogLevel.RestDetail:
                    if (_options.Value.RestDetail)
                        _innerLogger.Log(LLForDetail, message, args);
                    break;
                case KLogLevel.DINIT:
                    if (_options.Value.DINIT)
                        _innerLogger.Log(LLForDetail, message, args);
                    break;
                case KLogLevel.DINITDETAIL:
                    if (_options.Value.DINITDETAIL)
                        _innerLogger.Log(LLForDetail, message, args);
                    break;
                case KLogLevel.None:
                    // Do nothing
                    break;
                default:
                    // Unknown level, log as Information
                    _innerLogger.Log(LogLevel.Information, message, args);
                    break;
            }
        }

        public void LogInfo(string message, params object[] args) {
            Log(KLogLevel.Information, message, args);
        }

        public void LogDebug(string message, params object[] args) {
            Log(KLogLevel.Debug, message, args);
        }

        public void LogError(string message, params object[] args) {
            Log(KLogLevel.Error, message, args);
        }

        /// <summary>
        /// Custom KeeKee logging with predefined formatting.
        /// </summary>
        public void LogKeeKee(LogLevel level, string message, Exception? exception = null) {
            string formattedMessage = $"[KeeKee] {message}";
            _innerLogger.Log(level, exception, formattedMessage);
        }

        /// <summary>
        /// Log with a custom category instead of the default type category.
        /// </summary>
        public void LogWithCategory(LogLevel level, string category, string message, Exception? exception = null) {
            string formattedMessage = $"[{category}] {message}";
            _innerLogger.Log(level, exception, formattedMessage);
        }
    }
}