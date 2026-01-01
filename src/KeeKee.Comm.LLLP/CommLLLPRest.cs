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

using KeeKee.Comm;
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

        IRestHandler? m_paramGetHandler = null;
        IRestHandler? m_loginHandler = null;
        IRestHandler? m_logoutHandler = null;
        IRestHandler? m_teleportHandler = null;
        IRestHandler? m_exitHandler = null;
        IRestHandler? m_statHandler = null;

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

            m_loginHandler = m_restFactory.Create("/LLLP/action/login", null, ProcessPostLogin);
            m_logoutHandler = m_restFactory.Create("/LLLP/action/logout", null, ProcessPostLogout);
            m_teleportHandler = m_restFactory.Create("/LLLP/action/teleport", null, ProcessPostTeleport);
            m_exitHandler = m_restFactory.Create("/LLLP/action/exit", null, ProcessPostExit);

            m_paramGetHandler = m_restFactory.Create("/LLLP/status", ref connParams);
            m_statHandler = m_restFactory.Create("/LLLP/stats", m_comm.CommStatistics);

            await Task.CompletedTask;
        }

        // OBSOLETE: not used now that RestHandler does ParameterSets
        public OMVSD.OSD ProcessGet(Uri uri, string after) {
            OMVSD.OSDMap ret = new OMVSD.OSDMap();
            if (m_comm == null) {
                m_log.Log(KLogLevel.DBADERROR, "GET WITHOUT COMM CONNECTION!! URL=" + uri.ToString());
                return new OMVSD.OSD();
            }
            m_log.Log(KLogLevel.DCOMMDETAIL, "Parameter request: {0}", uri.ToString());
            string[] segments = after.Split('/');
            // the after should be "/NAME/param" where "NAME" is my apiname. If 'param' is there return one
            if (segments.Length > 2) {
                string paramName = segments[2];
                if (m_comm.ConnectionParams.Value.HasParameter(paramName)) {
                    ret.Add(paramName, new OMVSD.OSDString(m_comm.ConnectionParams.ParamString(paramName)));
                }
            } else {
                // they want the whole set
                ret = m_comm.ConnectionParams.GetDisplayable();
            }
            return ret;
        }

        public OMVSD.OSD ProcessPostLogin(IRestHandler handler, Uri uri, string after, OMVSD.OSD body,
                HttpListenerContext pContext, HttpListenerRequest pRequest, HttpListenerResponse pResponse) {

            OMVSD.OSDMap ret = new OMVSD.OSDMap();
            ParameterSet loginParams = new ParameterSet();
            try {
                OMVSD.OSDMap paramMap = (OMVSD.OSDMap)body;
                loginParams.Add(CommLLLP.FIELDFIRST, paramMap["LOGINFIRST"].AsString());
                loginParams.Add(CommLLLP.FIELDLAST, paramMap["LOGINLAST"].AsString());
                loginParams.Add(CommLLLP.FIELDPASS, paramMap["LOGINPASS"].AsString());
                loginParams.Add(CommLLLP.FIELDGRID, paramMap["LOGINGRID"].AsString());
                loginParams.Add(CommLLLP.FIELDSIM, paramMap["LOGINSIM"].AsString());
            } catch {
                m_log.Log(KLogLevel.DBADERROR, "MISFORMED POST REQUEST: ");
                ret.Add(RestHandler.RESTREQUESTERRORCODE, new OMVSD.OSDInteger(1));
                ret.Add(RestHandler.RESTREQUESTERRORMSG, new OMVSD.OSDString("Misformed POST request"));
                return ret;
            }

            try {
                if (!m_comm.Connect(loginParams)) {
                    m_log.Log(KLogLevel.DBADERROR, "CONNECT FAILED");
                    ret.Add(RestHandler.RESTREQUESTERRORCODE, new OMVSD.OSDInteger(1));
                    ret.Add(RestHandler.RESTREQUESTERRORMSG, new OMVSD.OSDString("Could not log in"));
                    return ret;
                }
            } catch (Exception e) {
                m_log.Log(KLogLevel.DBADERROR, "CONNECT EXCEPTION: " + e.ToString());
                ret.Add(RestHandler.RESTREQUESTERRORCODE, new OMVSD.OSDInteger(1));
                ret.Add(RestHandler.RESTREQUESTERRORMSG, new OMVSD.OSDString("Connection threw exception: " + e.ToString()));
                return ret;
            }

            return ret;
        }

        public OMVSD.OSD ProcessPostTeleport(IRestHandler handler, Uri uri, string after, OMVSD.OSD body,
                HttpListenerContext pContext, HttpListenerRequest pRequest, HttpListenerResponse pResponse) {
            OMVSD.OSDMap ret = new OMVSD.OSDMap();
            ParameterSet loginParams = new ParameterSet();
            try {
                OMVSD.OSDMap paramMap = (OMVSD.OSDMap)body;
                string dest = paramMap["DESTINATION"].AsString();
                m_log.Log(KLogLevel.DCOMMDETAIL, "Request to teleport to {0}", dest);
                m_comm.DoTeleport(dest);
            } catch (Exception e) {
                m_log.Log(KLogLevel.DBADERROR, "CONNECT EXCEPTION: " + e.ToString());
                ret.Add(RestHandler.RESTREQUESTERRORCODE, new OMVSD.OSDInteger(1));
                ret.Add(RestHandler.RESTREQUESTERRORMSG, new OMVSD.OSDString("Connection threw exception: " + e.ToString()));
                return ret;
            }
            return ret;
        }


        public OMVSD.OSD ProcessPostLogout(IRestHandler handler, Uri uri, string after, OMVSD.OSD body,
                HttpListenerContext pContext, HttpListenerRequest pRequest, HttpListenerResponse pResponse) {
            OMVSD.OSDMap ret = new OMVSD.OSDMap();
            m_comm.Disconnect();
            return ret;
        }

        public OMVSD.OSD ProcessPostExit(IRestHandler handler, Uri uri, string after, OMVSD.OSD body,
                HttpListenerContext pContext, HttpListenerRequest pRequest, HttpListenerResponse pResponse) {
            OMVSD.OSDMap ret = new OMVSD.OSDMap();
            LGB.KeepRunning = false;
            return ret;
        }
    }
}
