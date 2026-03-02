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

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using KeeKee.Config;
using KeeKee.Framework;
using KeeKee.Framework.Logging;
using KeeKee.Rest;

using OMVSD = OpenMetaverse.StructuredData;

namespace KeeKee.Session {

    /// POST http://127.0.0.1:port/api/Session/login    : take JSON body as parameters to use to login
    ///            parameters are: LOGINFIRST, LOGINLAST, LOGINPASS, LOGINGRID, LOGINSIM
    /// POST http://127.0.0.1:port/api/Session/logout   : perform a logout
    /// POST http://127.0.0.1:port/api/Session/exit     : exit the application
    /// 
    public class SessionManager : BackgroundService, IDisplayable {

        private readonly KLogger<SessionManager> m_log;
        private readonly IOptions<KeeKeeConfig> m_keeKeeConfig;

        RestHandler? m_loginHandler = null;
        RestHandler? m_logoutHandler = null;
        RestHandler? m_exitHandler = null;

        private readonly RestHandlerFactory m_RestHandlerFactory;

        public SessionManager(KLogger<SessionManager> pLog,
                        IOptions<KeeKeeConfig> pKeeKeeConfig,
                        RestHandlerFactory pRestHandlerFactory) {
            m_log = pLog;
            m_keeKeeConfig = pKeeKeeConfig;
            m_RestHandlerFactory = pRestHandlerFactory;

            m_log.Log(KLogLevel.DREST, "SessionManager constructor");
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken) {
            m_log.Log(KLogLevel.DREST, "SessionManager ExecuteAsync entered");

            m_loginHandler = m_RestHandlerFactory.CreateHandler<RestHandlerLogin>();
            m_logoutHandler = m_RestHandlerFactory.CreateHandler<RestHandlerLogout>();
            m_exitHandler = m_RestHandlerFactory.CreateHandler<RestHandlerExit>();
        }

        public OMVSD.OSD? GetDisplayable() {
            return null;
        }
    }
}
