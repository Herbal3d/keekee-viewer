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

using System.Net;
using System.Text.Json;

using KeeKee.Config;
using KeeKee.Framework.Logging;
using KeeKee.Rest;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using OMV = OpenMetaverse;
using OMVSD = OpenMetaverse.StructuredData;

namespace KeeKee.Comm.LLLP {

    /// <summary>
    /// Provides interface to LLLP communication stack.
    /// The LLLP stack makes a parameter set available which contains the necessary login
    /// values as well as the current state of the connection.
    /// This handles the following REST operations:
    /// GET http://127.0.0.0:port/api/LLLP/ : returns the JSON of the comm parameter block
    /// POST http://127.0.0.1:port/api/LLLP/connection/login    : take JSON body as parameters to use to login
    ///            parameters are: LOGINFIRST, LOGINLAST, LOGINPASS, LOGINGRID, LOGINSIM
    /// POST http://127.0.0.1:port/api/LLLP/connection/logout   : perform a logout
    /// POST http://127.0.0.1:port/api/LLLP/connection/exit     : exit the application
    /// POST http://127.0.0.1:port/api/LLLP/connection/teleport : teleport the user
    ///            parameter is DESTINATION
    /// GET https://127.0..1.1:port/api/LLLP/stats : get operation statistics
    /// </summary>
    public class CommLLLPRest : BackgroundService {
        private KLogger<CommLLLPRest> m_log;
        CommLLLP m_comm;
        public IOptions<CommConfig> ConnectionParams { get; private set; }
        RestHandlerFactory m_restFactory;

        IRestHandler? m_loginHandler = null;
        IRestHandler? m_logoutHandler = null;
        IRestHandler? m_teleportHandler = null;
        IRestHandler? m_exitHandler = null;
        IRestHandler? m_chatHandler = null;
        IRestHandler? m_statHandler = null;
        IRestHandler? m_paramGetHandler = null;

        public CommLLLPRest(KLogger<CommLLLPRest> pLog,
                        RestHandlerFactory pRestFactory,
                        CommLLLP pComm,
                        IOptions<CommConfig> pConnectionParams) {
            m_log = pLog;
            m_restFactory = pRestFactory;
            m_comm = pComm;
            ConnectionParams = pConnectionParams;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken) {
            m_log.LogInfo("CommLLLPRest starting.");

            m_loginHandler = m_restFactory.CreateHandler<RestHandlerLogin>();
            m_logoutHandler = m_restFactory.CreateHandler<RestHandlerLogout>();
            m_teleportHandler = m_restFactory.CreateHandler<RestHandlerTeleport>();
            m_exitHandler = m_restFactory.CreateHandler<RestHandlerExit>();
            m_chatHandler = m_restFactory.CreateHandler<RestHandlerChat>();

            // m_paramGetHandler = m_restFactory.Create("/LLLP/status", ref connParams);
            // m_statHandler = m_restFactory.Create("/LLLP/stats", m_comm.CommStatistics);

            await Task.CompletedTask;
        }
    }
}
