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
    public partial class ViewSplash : Form, IModule, IViewSplash {

    #region IModule
    protected string m_moduleName;
    public string ModuleName { get { return m_moduleName; } set { m_moduleName = value; } }

    protected KeeKeeBase m_lgb = null;
    public KeeKeeBase LGB { get { return m_lgb; } }

    public IAppParameters ModuleParams { get { return m_lgb.AppParams; } }

    public ViewSplash() {
        // default to the class name. The module code can set it to something else later.
        m_moduleName = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name;
    }

    // IModule.OnLoad
    public virtual void OnLoad(string modName, KeeKeeBase lgbase) {
        LogManager.Log.Log(LogLevel.DINIT, ModuleName + ".OnLoad()");
        m_moduleName = modName;
        m_lgb = lgbase;

        // slash happens very early
        InitializeComponent();
        Random rand = new Random();

        string[] ContentLines = new String[30];
        int line = 0;
        ContentLines[line++] = KeeKeeBase.ApplicationName + "  " + KeeKeeBase.ApplicationVersion
            + "                      http://lookingglassviewer.org/";
        ContentLines[line++] = "";
        ContentLines[line++] = "Copyright (c) 2025 Robert Adams (KeeKee)";
        ContentLines[line++] = "Copyright (c) 2001 Peter Dimov and Multi Media Ltd. (Boost)";
        ContentLines[line++] = "Copyright (c) Contributors http://opensimulator.org/ (OpenSimulator)";
        ContentLines[line++] = "Copyright (c) 2007-2009, http://openmetaverse.org (OpenMetaverse)";
        ContentLines[line++] = "Copyright (c) Contributors, http://idealistviewer.org/ (Idealist)";
        ContentLines[line++] = "Copyright (c) 2009 John Resig (jQuery)";
        ContentLines[line++] = "Copyright (c) 2009, Gareth Watts (SparkLines)";
        ContentLines[line++] = "Copyright (c) 2008 Linden Research, Inc. (Avatar mesh/artwork)";
        Array.Resize(ref ContentLines, line);
        this.WindowSplash.Lines = ContentLines;
        this.WindowSplash.Refresh();
        this.WindowSplash.BringToFront();

        Initialize();
        Visible = true;
        Show();
        Update();
    }

    // IModule.AfterAllModulesLoaded
    public virtual bool AfterAllModulesLoaded() {
        LogManager.Log.Log(LogLevel.DINIT, ModuleName + ".AfterAllModulesLoaded()");
        return true;
    }

    // IModule.Start
    public virtual void Start() {
        // when others are starting, we need to go away
        Visible = false;
        return;
    }

    // IModule.Stop
    public virtual void Stop() {
        return;
    }

    // IModule.PrepareForUnload
    public virtual bool PrepareForUnload() {
        return false;
    }
    #endregion IModule

    public ViewSplash(KeeKeeBase llgb) {
        m_lgb = llgb;

        InitializeComponent();
        Random rand = new Random();

        string[] ContentLines = new String[30];
        int line = 0;
        ContentLines[line++] = KeeKeeBase.ApplicationName + "  " + KeeKeeBase.ApplicationVersion
            + "                      http://lookingglassviewer.org/";
        ContentLines[line++] = "";
        ContentLines[line++] = "Copyright (c) 2008-2010 Robert Adams (KeeKee)";
        ContentLines[line++] = "Copyright (c) 2000-2006 Torus Knot Software Ltd (Ogre)";
        ContentLines[line++] = "Copyright (c) 2001 Peter Dimov and Multi Media Ltd. (Boost)";
        ContentLines[line++] = "Copyright (c) Contributors http://opensimulator.org/ (OpenSimulator)";
        ContentLines[line++] = "Copyright (c) 2007-2009, http://openmetaverse.org (OpenMetaverse)";
        ContentLines[line++] = "Copyright (c) Contributors, http://idealistviewer.org/ (Idealist)";
        ContentLines[line++] = "Copyright (c) 2009 Xavier Verguín González (SkyX)";
        ContentLines[line++] = "Copyright (c) 2009 John Resig (jQuery)";
        ContentLines[line++] = "Copyright (c) 2009, Gareth Watts (SparkLines)";
        ContentLines[line++] = "Copyright (c) 2004-2008 Matthew Holmes, Dan Moorehead, Rob Loach, C.J. Adams-Collier (Prebuild v2.0.4)";
        ContentLines[line++] = "Copyright (c) 2008 Linden Research, Inc. (Avatar mesh/artwork)";
        Array.Resize(ref ContentLines, line);
        this.WindowSplash.Lines = ContentLines;
        this.WindowSplash.Refresh();
        this.WindowSplash.BringToFront();
    }

    // Put text in the initialization progress place in the splash screen
    public void InitializationProgress(String progress) {
        this.splashState.Text = progress;
        this.splashState.Refresh();
    }

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
