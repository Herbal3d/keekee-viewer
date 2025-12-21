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
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using KeeKee;
using KeeKee.Framework;
using KeeKee.Framework.Logging;
using KeeKee.Framework.Modules;
using KeeKee.Framework.Parameters;
using KeeKee.Framework.WorkQueue;
using OMV = OpenMetaverse;

namespace KeeKee.Rest {

    /// <summary>
    /// RestManager makes available HTTP connections to static and dynamic information.
    /// RestManager provides two functions: static web pages for ui and script support and
    /// REST interface capabilities for services within KeeKee to get and present data.
    /// 
    /// The static interface presents two sets of URLs which are mapped into the filesystem:
    /// http://127.0.0.1:9144/std/xxx : 'standard' pages which are common libraries
    /// This maps to the directory "Rest.Manager.UIContentDir" which defaults to
    /// "BINDIR/../UI/std/"
    /// http://127.0.0.1:9144/static/xxx : ui pages which can be 'skinned'
    /// This maps to the directory "Rest.Manager.UIContentDir"/"Rest.Manaager.Skin" which
    /// defaults to "BINDIR/../UI/Default/".
    /// 
    /// The dynamic content is created by servers creating instances of RestHandler.
    /// This creates URLs like:
    /// http://127.0.0.1:9144/api/SERVICE/xxx
    /// where 'service' is the name of teh service and 'xxx' is whatever it wants.
    /// These implement GET and POST operations of JSON formatted data.
    /// </summary>
public class RestManager : ModuleBase, IInstance<RestManager> {
    private ILog m_log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);

#pragma warning disable 414 // I know these are set and never referenced
    RestHandler m_staticHandler = null;
    RestHandler m_stdHandler = null;
    RestHandler m_faviconHandler = null;
#pragma warning restore 414

    public const string MIMEDEFAULT = "text/html";

    protected int m_port;
    public int Port { get { return m_port; } }
    HttpListener m_listener;
    Thread m_listenerThread;
    List<RestHandler> m_handlers = new List<RestHandler>();

    // Some system wide rest handlers to make information available
    RestHandler m_workQueueRestHandler = null;
    RestHandler m_paramDefaultRestHandler = null;
    RestHandler m_paramIniRestHandler = null;
    RestHandler m_paramUserRestHandler = null;
    RestHandler m_paramOverrideRestHandler = null;

    // return the full base URL with the port added
    protected string m_baseURL;
    public string BaseURL { get { return m_baseURL; } }

    private static RestManager m_instance = null;
    public static RestManager Instance {
        get {
            if (m_instance == null) {
                throw new KeeKeeException("CALLING FOR RESTMANAGER INSTANCE BEFORE SET!!!");
            }
            return m_instance;
        }
    }

    public RestManager() {
        m_instance = this;
    }

    #region IModule methods
    public override void OnLoad(string modName, KeeKeeBase lgbase) {
        base.OnLoad(modName, lgbase);
        ModuleParams.AddDefaultParameter(m_moduleName + ".Port", "9144",
                    "Local port used for rest interfaces");
        ModuleParams.AddDefaultParameter(m_moduleName + ".BaseURL", "http://127.0.0.1",
                    "Base URL for rest interfaces");
        ModuleParams.AddDefaultParameter(m_moduleName + ".CSSLocalURL", "/std/KeeKee.css",
                    "CSS file for rest display");
        ModuleParams.AddDefaultParameter(m_moduleName + ".UIContentDir", 
                    Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "./KeeKeeUI"),
                    "Directory for static HTML content");
        ModuleParams.AddDefaultParameter(m_moduleName + ".Skin", "Default",
                    "If specified, the subdirectory under StaticContentDir to take files from");
    }

    override public bool AfterAllModulesLoaded() {
        m_log.Log(LogLevel.DINIT, "entered AfterAllModulesLoaded()");

        // m_baseURL = "http://127.0.0.1:PORT/"
        m_port = ModuleParams.ParamInt(m_moduleName + ".Port");
        m_baseURL = ModuleParams.ParamString(m_moduleName + ".BaseURL");
        m_baseURL = m_baseURL + ":" + m_port.ToString();

        m_listener = new HttpListener();
        m_listener.Prefixes.Add(m_baseURL + "/");

        // m_baseUIDIR = "/.../bin/KeeKeeUI/"
        string baseUIDir = ModuleParams.ParamString(m_moduleName + ".UIContentDir");
        if (!baseUIDir.EndsWith("/")) baseUIDir += "/";

        // things referenced as static are from the skinning directory below the UI dir
        // m_staticDir = "/.../bin/KeeKeeUI/Default/"
        string staticDir = baseUIDir;
        if (ModuleParams.HasParameter(m_moduleName + ".Skin")) {
            string skinName = ModuleParams.ParamString(m_moduleName + ".Skin");
            skinName.Replace("/", "");  // skin names shouldn't fool with directories
            skinName.Replace("\\", "");
            skinName.Replace("..", "");
            staticDir = staticDir + skinName;
        }
        if (!staticDir.EndsWith("/")) staticDir += "/";

        // stdDir = "/.../bin/KeeKeeUI/std/";
        string stdDir = baseUIDir + "std/";

        m_log.Log(LogLevel.DINITDETAIL, "Registering FileHandler {0} -> {1}", "/static/", staticDir);
        m_staticHandler = new RestHandler("/static/", staticDir);

        m_log.Log(LogLevel.DINITDETAIL, "Registering FileHandler {0} -> {1}", "/std/", stdDir);
        m_stdHandler = new RestHandler("/std/", stdDir);
        m_faviconHandler = new RestHandler("/favicon.ico", baseUIDir);

        // some Framework structures that can be referenced
        m_log.Log(LogLevel.DINITDETAIL, "Registering work queue stats at 'api/stats/workQueues'");
        m_workQueueRestHandler = new RestHandler("/stats/workQueues", WorkQueueManager.Instance);

        m_log.Log(LogLevel.DINITDETAIL, "Registering parmeters at 'api/params/Default' and 'Ini', 'User', 'Override'");
        m_paramDefaultRestHandler = new RestHandler("/params/Default", m_lgb.AppParams.DefaultParameters);
        m_paramIniRestHandler = new RestHandler("/params/Ini", m_lgb.AppParams.IniParameters);
        m_paramUserRestHandler = new RestHandler("/params/User", m_lgb.AppParams.UserParameters);
        m_paramOverrideRestHandler = new RestHandler("/params/Override", m_lgb.AppParams.OverrideParameters);

        m_log.Log(LogLevel.DINIT, "exiting AfterAllModulesLoaded()");
        return true;
    }

    // Routine for HttpServer.ILogWriter
    public void Write(object source, /*HttpServer.LogPrio prio,*/ string msg) {
        /*
        LogLevel level = LogLevel.DREST;
        if (prio == HttpServer.LogPrio.Debug || prio == HttpServer.LogPrio.Info) {
            level = LogLevel.DRESTDETAIL;
        }
        m_log.Log(level, msg);
         */
        return;
    }

    public override void Start() {
        base.Start();
        m_log.Log(LogLevel.DRESTDETAIL, "Start(). Starting listening");
        m_listener.Start();
        m_listenerThread = new Thread(LoopForInput);
        m_listenerThread.Name = "REST Input";
        m_listenerThread.Start();
    }

    override public void Stop() {
        return;
    }
    #endregion IModule methods

    public void RegisterListener(RestHandler handler) {
        m_log.Log(LogLevel.DRESTDETAIL, "Registering prefix {0}", handler.m_prefix);
        m_handlers.Add(handler);
    }

    public void LoopForInput() {
        while (m_lgb.KeepRunning) {
            HttpListenerContext context = m_listener.GetContext();
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;
            string absURL = request.Url.AbsolutePath.ToLower();
            RestManager.Instance.m_log.Log(LogLevel.DRESTDETAIL, "HTTP request for {0}", absURL);
            RestHandler thisHandler = null;
            foreach (RestHandler rh in RestManager.Instance.m_handlers) {
                if (absURL.StartsWith(rh.m_prefix.ToLower())) {
                    thisHandler = rh;
                    break;
                }
            }
            if (thisHandler != null) {
                string afterString = absURL.Substring(thisHandler.m_prefix.Length);
                thisHandler.m_context = context;
                thisHandler.m_request = request;
                thisHandler.m_response = response;
                thisHandler.GetPostAsync(afterString);
            }
            else {
                RestManager.Instance.m_log.Log(LogLevel.DRESTDETAIL, "Request not processed because no matching handler");
            }
        }
    }

    #region HTML Helper Routines

    public delegate void ConstructResponseRoutine(ref StringBuilder buff);

    /// <summary>
    /// Just like 'ConstructResponse' but has very simplified headers. Good for AJAX reponsses.
    /// </summary>
    /// <param name="context">The request information</param>
    /// <param name="title">The title for the HTML page</param>
    /// <param name="addHeader">Called to add HTML to the header. May be null.</param>
    /// <param name="addContent">Called to add HTML to the body. May be null.</param>
    public void ConstructSimpleResponse(HttpListenerResponse context, 
                    string contentType,
                    ConstructResponseRoutine addContent) {
        StringBuilder buff = new StringBuilder();
        try {
            context.ContentType = contentType == null ? MIMEDEFAULT : contentType;
            context.AddHeader("Server", KeeKeeBase.ApplicationName);
            context.AddHeader("Cache-Control", "no-cache");
            // context.Connection = ConnectionType.Close;

            if (addContent != null) addContent(ref buff);
            context.StatusCode = (int)HttpStatusCode.OK;
        }
        catch {
            buff = new StringBuilder();
            buff.Append("<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Transitional//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd\">\r\n");
            buff.Append("<html xmlns=\"http://www.w3.org/1999/xhtml\">\r\n");
            buff.Append("<head></head><body></body></html>\r\n");
            context.StatusCode = (int)HttpStatusCode.InternalServerError;
        }

        byte[] encodedBuff = System.Text.Encoding.UTF8.GetBytes(buff.ToString());
        
        context.ContentLength64 = encodedBuff.Length;
        System.IO.Stream output = context.OutputStream;
        output.Write(encodedBuff, 0, encodedBuff.Length);
        output.Close();
        return;
    }

    /// <summary>
    /// Construct a response that is all abbout errors
    /// </summary>
    /// <param name="context">The request information</param>
    /// <param name="errCode">The HTTP error code toreturn</param>
    /// <param name="addContent">Called to add HTML to the body. May be null.</param>
    public void ConstructErrorResponse(HttpListenerResponse context, 
                    HttpStatusCode errCode, 
                    ConstructResponseRoutine addContent) {
        StringBuilder buff = new StringBuilder();
        try {
            context.ContentType = MIMEDEFAULT;
            context.AddHeader("Server", KeeKeeBase.ApplicationName);
            // context.Connection = ConnectionType.Close;

            buff.Append("<body>\r\n");
            if (addContent != null) addContent(ref buff);
            buff.Append("</body>\r\n");
            buff.Append("</html>\r\n\r\n");
            context.StatusCode = (int)errCode;
        }
        catch {
            buff = new StringBuilder();
            buff.Append("<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Transitional//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd\">\r\n");
            buff.Append("<html xmlns=\"http://www.w3.org/1999/xhtml\">\r\n");
            buff.Append("<head></head><body></body></html>\r\n");
            context.StatusCode = (int)HttpStatusCode.InternalServerError;
        }

        byte[] encodedBuff = System.Text.Encoding.UTF8.GetBytes(buff.ToString());
        
        context.ContentLength64 = encodedBuff.Length;
        System.IO.Stream output = context.OutputStream;
        output.Write(encodedBuff, 0, encodedBuff.Length);
        output.Close();
        return;
    }


    #endregion HTML Helper Routines
    
}
}
