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
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using KeeKee.Framework;
using KeeKee.Framework.Logging;
using KeeKee.Framework.Modules;
using KeeKee.Framework.Parameters;

namespace KeeKee.View {
    public partial class FormAvatars : Form, IModule, IViewAvatar {

    #region IModule
    protected string m_moduleName;
    public string ModuleName { get { return m_moduleName; } set { m_moduleName = value; } }

    protected KeeKeeBase m_lgb = null;
    public KeeKeeBase LGB { get { return m_lgb; } }

    public IAppParameters ModuleParams { get { return m_lgb.AppParams; } }

    public FormAvatars() {
        // default to the class name. The module code can set it to something else later.
        m_moduleName = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name;
    }

    // IModule.OnLoad
    public virtual void OnLoad(string modName, KeeKeeBase lgbase) {
        LogManager.Log.Log(LogLevel.DINIT, ModuleName + ".OnLoad()");
        m_moduleName = modName;
        m_lgb = lgbase;
    }

    // IModule.AfterAllModulesLoaded
    public virtual bool AfterAllModulesLoaded() {
        LogManager.Log.Log(LogLevel.DINIT, ModuleName + ".AfterAllModulesLoaded()");

        InitializeComponent();
        Random rand = new Random();

        string baseURL = LGB.AppParams.ParamString("RestManager.BaseURL");
        string portNum = LGB.AppParams.ParamString("RestManager.Port");
        string avatarURL = baseURL + ":" + portNum + "/static/ViewAvatars.html?xx=" + rand.Next(1,999999).ToString();
        this.WindowAvatars.Url = new Uri(avatarURL);
        this.WindowAvatars.ScriptErrorsSuppressed = false;  // DEBUG
        this.WindowAvatars.Refresh();
        this.WindowAvatars.BringToFront();
        this.WindowAvatars.Focus();
        return true;
    }

    // IModule.Start
    public virtual void Start() {
        this.Initialize();
        this.Visible = true;
        return;
    }

    // IModule.Stop
    public virtual void Stop() {
        this.Shutdown();
        return;
    }

    // IModule.PrepareForUnload
    public virtual bool PrepareForUnload() {
        return false;
    }
    #endregion IModule

    public void Initialize() {
    }

    public void Shutdown() {
        if (this.InvokeRequired) {
            BeginInvoke((MethodInvoker)delegate() { this.Close(); });
        }
        else {
            this.Close();
        }
    }
}
}
