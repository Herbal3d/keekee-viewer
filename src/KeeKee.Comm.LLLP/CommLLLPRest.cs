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
using KeeKee;
using KeeKee.Comm;
using KeeKee.Framework.Logging;
using KeeKee.Rest;
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
    ///    parameters are: LOGINFIRST, LOGINLAST, LOGINPASS, LOGINGRID, LOGINSIM
    /// POST http://127.0.0.1:port/api/LLLP/connection/logout   : perform a logout
    /// POST http://127.0.0.1:port/api/LLLP/connection/exit     : exit the application
    /// POST http://127.0.0.1:port/api/LLLP/connection/teleport : teleport the user
    ///    parameter is DESTINATION
    /// </summary>
public class CommLLLPRest : {
    private KLogger<CommLLLPRest> m_log;
    CommLLLP m_comm = null;
    string m_apiName;
    RestHandler m_paramGetHandler = null;
    RestHandler m_actionHandler = null;

    public CommLLLPRest() {
    }

    public override void OnLoad(string name, KeeKeeBase lgbase) {
        base.OnLoad(name, lgbase);
        m_apiName = "LLLP";
        ModuleParams.AddDefaultParameter(ModuleName + ".Comm.Name", "Comm", "Name of comm module to connect to");
        ModuleParams.AddDefaultParameter(ModuleName + ".APIName", m_apiName, "Name of api for this comm control");
    }

    public override void Start() {
        string commName = ModuleParams.ParamString(ModuleName + ".Comm.Name");
        try {
            m_comm = (CommLLLP)ModuleManager.Instance.Module(commName);
        }
        catch (Exception e) {
            m_log.Log(kLogLevel.DBADERROR, "CommLLLPRest COULD NOT CONNECT TO COMM MODULE NAMED " + commName);
            m_log.Log(kLogLevel.DBADERROR, "CommLLLPRest error = " + e.ToString());
        }
        try {
            m_apiName = ModuleParams.ParamString(ModuleName + ".APIName");

            ParameterSet connParams = m_comm.ConnectionParams;
            m_paramGetHandler = new RestHandler("/" + m_apiName + "/status", ref connParams, false);
            m_actionHandler = new RestHandler("/" + m_apiName + "/connect", null, ProcessPost);
        }
        catch (Exception e) {
            m_log.Log(kLogLevel.DBADERROR, "CommLLLPRest COULD NOT REGISTER REST OPERATION: " + e.ToString());
        }
    }

    // OBSOLETE: not used now that RestHandler does ParameterSets
    public OMVSD.OSD ProcessGet(Uri uri, string after) {
        OMVSD.OSDMap ret = new OMVSD.OSDMap();
        if (m_comm == null) {
            m_log.Log(kLogLevel.DBADERROR, "GET WITHOUT COMM CONNECTION!! URL=" + uri.ToString());
            return new OMVSD.OSD();
        }
        m_log.Log(kLogLevel.DCOMMDETAIL, "Parameter request: {0}", uri.ToString());
        string[] segments = after.Split('/');
        // the after should be "/NAME/param" where "NAME" is my apiname. If 'param' is there return one
        if (segments.Length > 2) {
            string paramName = segments[2];
            if (m_comm.ConnectionParams.HasParameter(paramName)) {
                ret.Add(paramName, new OMVSD.OSDString(m_comm.ConnectionParams.ParamString(paramName)));
            }
        }
        else {
            // they want the whole set
            ret = m_comm.ConnectionParams.GetDisplayable();
        }
        return ret;
    }

    /// <summary>
    /// Posting to this communication instance. The URI comes in as "/api/MYNAME/ACTION" where
    /// ACTION is "login", "logout".
    /// </summary>
    /// <param name="uri"></param>
    /// <param name="body"></param>
    /// <returns></returns>
    public OMVSD.OSD ProcessPost(RestHandler handler, Uri uri, string after, OMVSD.OSD body) {
        OMVSD.OSDMap ret = new OMVSD.OSDMap();
        if (m_comm == null) {
            m_log.Log(kLogLevel.DBADERROR, "POST WITHOUT COMM CONNECTION!! URL=" + uri.ToString());
            return new OMVSD.OSD();
        }
        m_log.Log(kLogLevel.DCOMMDETAIL, "Post action: {0}", uri.ToString());
        switch (after) {
            case "/login":
                ret = PostActionLogin(body);
                break;
            case "/teleport":
                ret = PostActionTeleport(body);
                break;
            case "/logout":
                ret = PostActionLogout(body);
                break;
            case "/exit":
                ret = PostActionExit(body);
                break;
            default:
                m_log.Log(kLogLevel.DBADERROR, "UNKNOWN ACTION: " + uri.ToString());
                ret.Add(RestHandler.RESTREQUESTERRORCODE, new OMVSD.OSDInteger(1));
                ret.Add(RestHandler.RESTREQUESTERRORMSG, new OMVSD.OSDString("Unknown action"));
                break;
        }
        return ret;
    }

    private OMVSD.OSDMap PostActionLogin(OMVSD.OSD body) {
        OMVSD.OSDMap ret = new OMVSD.OSDMap();
        ParameterSet loginParams = new ParameterSet();
        try {
            OMVSD.OSDMap paramMap = (OMVSD.OSDMap)body;
            loginParams.Add(CommLLLP.FIELDFIRST, paramMap["LOGINFIRST"].AsString());
            loginParams.Add(CommLLLP.FIELDLAST, paramMap["LOGINLAST"].AsString());
            loginParams.Add(CommLLLP.FIELDPASS, paramMap["LOGINPASS"].AsString());
            loginParams.Add(CommLLLP.FIELDGRID, paramMap["LOGINGRID"].AsString());
            loginParams.Add(CommLLLP.FIELDSIM, paramMap["LOGINSIM"].AsString());
        }
        catch {
            m_log.Log(kLogLevel.DBADERROR, "MISFORMED POST REQUEST: ");
            ret.Add(RestHandler.RESTREQUESTERRORCODE, new OMVSD.OSDInteger(1));
            ret.Add(RestHandler.RESTREQUESTERRORMSG, new OMVSD.OSDString("Misformed POST request"));
            return ret;
        }

        try {
            if (!m_comm.Connect(loginParams)) {
                m_log.Log(kLogLevel.DBADERROR, "CONNECT FAILED");
                ret.Add(RestHandler.RESTREQUESTERRORCODE, new OMVSD.OSDInteger(1));
                ret.Add(RestHandler.RESTREQUESTERRORMSG, new OMVSD.OSDString("Could not log in"));
                return ret;
            }
        }
        catch (Exception e) {
            m_log.Log(kLogLevel.DBADERROR, "CONNECT EXCEPTION: " + e.ToString());
            ret.Add(RestHandler.RESTREQUESTERRORCODE, new OMVSD.OSDInteger(1));
            ret.Add(RestHandler.RESTREQUESTERRORMSG, new OMVSD.OSDString("Connection threw exception: " + e.ToString()));
            return ret;
        }

        return ret;
    }

    private OMVSD.OSDMap PostActionTeleport(OMVSD.OSD body) {
        OMVSD.OSDMap ret = new OMVSD.OSDMap();
        ParameterSet loginParams = new ParameterSet();
        try {
            OMVSD.OSDMap paramMap = (OMVSD.OSDMap)body;
            string dest = paramMap["DESTINATION"].AsString();
            m_log.Log(kLogLevel.DCOMMDETAIL, "Request to teleport to {0}", dest);
            m_comm.DoTeleport(dest);
        }
        catch (Exception e) {
            m_log.Log(kLogLevel.DBADERROR, "CONNECT EXCEPTION: " + e.ToString());
            ret.Add(RestHandler.RESTREQUESTERRORCODE, new OMVSD.OSDInteger(1));
            ret.Add(RestHandler.RESTREQUESTERRORMSG, new OMVSD.OSDString("Connection threw exception: " + e.ToString()));
            return ret;
        }
        return ret;
    }


    private OMVSD.OSDMap PostActionLogout(OMVSD.OSD body) {
        OMVSD.OSDMap ret = new OMVSD.OSDMap();
        m_comm.Disconnect();
        return ret;
    }

    private OMVSD.OSDMap PostActionExit(OMVSD.OSD body) {
        OMVSD.OSDMap ret = new OMVSD.OSDMap();
        LGB.KeepRunning = false;
        return ret;
    }
}
}
