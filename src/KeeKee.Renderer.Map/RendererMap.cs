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
using KeeKee.Framework;
using KeeKee.Framework.Logging;
using KeeKee.Framework.Modules;
using KeeKee.Framework.Parameters;
using KeeKee.Renderer;
using KeeKee.World;
using OMV = OpenMetaverse;

namespace KeeKee.Renderer.Map {
    /// <summary>
    /// A renderer that will someday make map pages of the sim.
    /// At the moment it's just a null renderer.
    /// </summary>
public class RendererMap : IModule, IRenderProvider {
    private ILog m_log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);

    #region IModule
    protected string m_moduleName;
    public string ModuleName { get { return m_moduleName; } set { m_moduleName = value; } }

    protected KeeKeeBase m_lgb = null;
    public KeeKeeBase LGB { get { return m_lgb; } }

    public IAppParameters ModuleParams { get { return m_lgb.AppParams; } }

    public RendererMap() {
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
        m_userInterface = new UserInterfaceNull();
        return true;
    }

    // IModule.Start
    public virtual void Start() {
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

    #region IRenderProvider
    IUserInterfaceProvider m_userInterface = null;
    public IUserInterfaceProvider UserInterface { 
        get { return m_userInterface; } 
    }

    // entry for main thread for rendering. Return false if you don't need it.
    public bool RendererThread() {
        return false;
    }
    // entry for rendering one frame. An alternate to the above thread method
    public bool RenderOneFrame(bool pump, int len) {
        return true;
    }

    //=================================================================
    // Set the entity to be rendered
    public void Render(IEntity ent) {
        return;
    }
    public void RenderUpdate(IEntity ent, UpdateCodes what) {
        return;
    }
    public void UnRender(IEntity ent) {
        return;
    }

    // tell the renderer about the camera position
    public void UpdateCamera(CameraControl cam) {
        return;
    }
    public void UpdateEnvironmentalLights(EntityLight sun, EntityLight moon) {
        return;
    }

    // Given the current mouse position, return a point in the world
    public OMV.Vector3d SelectPoint() {
        return new OMV.Vector3d(0d, 0d, 0d);
    }

    // rendering specific information for placing in  the view
    public void MapRegionIntoView(RegionContextBase rcontext) {
        return;
    }

    // Set one region as the focus of display
    public void SetFocusRegion(RegionContextBase rcontext) {
        return;
    }

    // something about the terrain has changed, do some updating
    public void UpdateTerrain(RegionContextBase wcontext) {
        return;
    }
    #endregion IRenderProvider
}
}
