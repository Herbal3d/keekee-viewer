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
using System.Threading;
using System.Windows.Forms;
using KeeKee;
using KeeKee.Framework.Logging;
using KeeKee.Framework.Modules;
using KeeKee.Framework.Parameters;
using KeeKee.Renderer;

namespace KeeKee.View {
public partial class ViewWindow : Form, IModule, IViewWindow {
    private ILog m_log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);

    // private Panel m_renderPanel;
    private IRenderProvider m_renderer;
    private System.Threading.Timer m_refreshTimer;
    private int m_framesPerSec;
    private int m_frameTimeMs;      // 1000/framesPerSec
    private int m_frameAllowanceMs; // maz(1000/framesPerSec - 30, 10) time allowed for frame plus extra work

    private IUserInterfaceProvider m_UILink = null;
    private bool m_MouseIn = false;     // true if mouse is over our window
    private float m_MouseLastX = -3456f;
    private float m_MouseLastY = -3456f;

    #region IModule
    protected string m_moduleName;
    public string ModuleName { get { return m_moduleName; } set { m_moduleName = value; } }

    protected KeeKeeBase m_lgb = null;
    public KeeKeeBase LGB { get { return m_lgb; } }

    public IAppParameters ModuleParams { get { return m_lgb.AppParams; } }

    public ViewWindow() {
        // default to the class name. The module code can set it to something else later.
        m_moduleName = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name;
    }

    // IModule.OnLoad
    public virtual void OnLoad(string modName, KeeKeeBase lgbase) {
        LogManager.Log.Log(LogLevel.DINIT, ModuleName + ".OnLoad()");
        m_moduleName = modName;
        m_lgb = lgbase;

        m_lgb.AppParams.AddDefaultParameter("ViewerWindow.Renderer.Name", "Renderer", "The renderer we will get UI from");
        m_lgb.AppParams.AddDefaultParameter("ViewerWindow.FramesPerSec", "15", "The rate to throttle frame rendering");

        InitializeComponent();

        // find the window and put the handle into the parameters so the rendering system can find it
        Control[] subControls = this.Controls.Find("LGWindow", true);
        if (subControls.Length == 1) {
            Control windowPanel = subControls[0];
            string wHandle = windowPanel.Handle.ToString();
            m_log.Log(LogLevel.DRADEGASTDETAIL, "Connecting to external window {0}, w={1}, h={2}",
                wHandle, windowPanel.Width, windowPanel.Height);
            LGB.AppParams.AddDefaultParameter("Renderer.Ogre.ExternalWindow.Handle",
                windowPanel.Handle.ToString(),
                "The window handle to use for our rendering");
            LGB.AppParams.AddDefaultParameter("Renderer.Ogre.ExternalWindow.Width",
                windowPanel.Width.ToString(), "width of external window");
            LGB.AppParams.AddDefaultParameter("Renderer.Ogre.ExternalWindow.Height",
                windowPanel.Height.ToString(), "Height of external window");
        }
        else {
            m_log.Log(LogLevel.DBADERROR, "Could not find window control on dialog");
            throw new Exception("Could not find window control on dialog");
        }
    }

    // IModule.AfterAllModulesLoaded
    public virtual bool AfterAllModulesLoaded() {
        LogManager.Log.Log(LogLevel.DINIT, ModuleName + ".AfterAllModulesLoaded()");
        return true;
    }

    // IModule.Start
    public virtual void Start() {
        LogManager.Log.Log(LogLevel.DINIT, "ControlViews.Start(): Initializing ViewWindow");
        Initialize();
        Visible = true;
        Show();
        return;
    }

    // IModule.Stop
    public virtual void Stop() {
        LogManager.Log.Log(LogLevel.DINIT, "ControlViews.Stop(): Stopping ViewWindow");
        Shutdown();
        return;
    }

    // IModule.PrepareForUnload
    public virtual bool PrepareForUnload() {
        return false;
    }
    #endregion IModule

    // Called after KeeKee is initialized
    public void Initialize() {
        try {
            // get a handle to the renderer module in KeeKee
            string rendererName = m_lgb.AppParams.ParamString("ViewerWindow.Renderer.Name");
            m_framesPerSec = Math.Min(100, Math.Max(1, m_lgb.AppParams.ParamInt("ViewerWindow.FramesPerSec")));
            m_frameTimeMs = 1000 / m_framesPerSec;
            m_frameAllowanceMs = Math.Max(m_framesPerSec - 30, 10);
            m_renderer = (IRenderProvider)m_lgb.ModManager.Module(rendererName);
            m_log.Log(LogLevel.DVIEWDETAIL, "Initialize. Connecting to renderer {0} at {1}fps",
                            m_renderer, m_framesPerSec);

            // the link to the renderer for display is also a link to the user interface routines
            m_UILink = m_renderer.UserInterface;

            m_refreshTimer = new System.Threading.Timer(delegate(Object param) {
                this.LGWindow.Invalidate();
                }, null, 2000, m_frameTimeMs);
        }
        catch (Exception e) {
            m_log.Log(LogLevel.DBADERROR, "Initialize. exception: {0}", e.ToString());
            throw new KeeKeeException("Exception initializing view");
        }

    }
    
    private void ViewWindow_Load(object sender, EventArgs e) {
    }

    public void Shutdown() {
        // Stop KeeKee
        m_lgb.KeepRunning = false;
        // Make sure I don't update any more
        if (m_refreshTimer != null) {
            m_refreshTimer.Dispose();
            m_refreshTimer = null;
        }
        // Those forms events are needed either
    }

    public void LGWindow_Paint(object sender, PaintEventArgs e) {
        try {
            if (this.InvokeRequired) {
                return; // just wait for the next tick
                // BeginInvoke((MethodInvoker)delegate() { m_renderer.RenderOneFrame(false, m_frameAllowanceMs); });
                // m_log.Log(LogLevel.DVIEW, "LGWindow_Paint: did BeginInvoke");
            }
            else {
                m_renderer.RenderOneFrame(false, m_frameAllowanceMs);
            }
        }
        catch (Exception err) {
            m_log.Log(LogLevel.DBADERROR, "LGWindow_Paint: EXCEPTION: {0}", err);
        }
        if (!m_lgb.KeepRunning) {
            if (this.InvokeRequired) {
                BeginInvoke((MethodInvoker)delegate() { this.Close(); });
            }
            else {
                this.Close();
            }
        }
        return;
    }

    public void LGWindow_Resize(object sender, EventArgs e) {
        return;
    }

    private void LGWindow_MouseDown(object sender, MouseEventArgs e) {
        if (m_UILink != null && m_MouseIn) {
            int butn = ConvertMouseButtonCode(e.Button);
            m_UILink.ReceiveUserIO(ReceiveUserIOInputEventTypeCode.MouseButtonDown, butn, 0f, 0f);
        }
    }

    private void LGWindow_MouseMove(object sender, MouseEventArgs e) {
        if (m_UILink != null && m_MouseIn) {
            // ReceiveUserIO wants relative mouse movement. Convert abs to rel
            int butn = ConvertMouseButtonCode(e.Button);
            if (m_MouseLastX == -3456f) m_MouseLastX = e.X;
            if (m_MouseLastY == -3456f) m_MouseLastY = e.Y;
            m_UILink.ReceiveUserIO(ReceiveUserIOInputEventTypeCode.MouseMove, butn,
                            e.X - m_MouseLastX, e.Y - m_MouseLastY);
            m_MouseLastX = e.X;
            m_MouseLastY = e.Y;
        }
    }

    private void LGWindow_MouseLeave(object sender, EventArgs e) {
        m_MouseIn = false;
    }

    private void LGWindow_MouseEnter(object sender, EventArgs e) {
        m_MouseIn = true;
    }

    private void LGWindow_MouseUp(object sender, MouseEventArgs e) {
        if (m_UILink != null) {
            int butn = ConvertMouseButtonCode(e.Button);
            m_UILink.ReceiveUserIO(ReceiveUserIOInputEventTypeCode.MouseButtonUp, butn, 0f, 0f);
        }
    }

    private void LGWindow_KeyDown(object sender, KeyEventArgs e) {
        if (m_UILink != null) {
            // LogManager.Log.Log(LogLevel.DVIEWDETAIL, "ViewWindow.LGWindow_KeyDown: k={0}", e.KeyCode);
            m_UILink.ReceiveUserIO(ReceiveUserIOInputEventTypeCode.KeyPress, (int)e.KeyCode, 0f, 0f);
        }
    }

    private void LGWindow_KeyUp(object sender, KeyEventArgs e) {
        if (m_UILink != null) {
            // LogManager.Log.Log(LogLevel.DVIEWDETAIL, "ViewWindow.LGWindow_KeyUp: k={0}", e.KeyCode);
            m_UILink.ReceiveUserIO(ReceiveUserIOInputEventTypeCode.KeyRelease, (int)e.KeyCode, 0f, 0f);
        }
    }

    private int ConvertMouseButtonCode(MouseButtons inCode) {
        if ((inCode & MouseButtons.Left) != 0) return (int)ReceiveUserIOMouseButtonCode.Left;
        if ((inCode & MouseButtons.Right) != 0) return (int)ReceiveUserIOMouseButtonCode.Right;
        if ((inCode & MouseButtons.Middle) != 0) return (int)ReceiveUserIOMouseButtonCode.Middle;
        return 0;
    }

}
}
