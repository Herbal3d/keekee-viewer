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

using KeeKee.Framework;
using KeeKee.Framework.Logging;
using KeeKee.Renderer;
using KeeKee.World;

using OMV = OpenMetaverse;

namespace KeeKee.Renderer.Map {
    /// <summary>
    /// A renderer that will someday make map pages of the sim.
    /// At the moment it's just a null renderer.
    /// </summary>
    public class RendererMap : IRenderProvider {
        private KLogger<RendererMap> m_log;

        public RendererMap(KLogger<RendererMap> pLog) {
            m_log = pLog;
            // default to the class name. The module code can set it to something else later.
            UserInterface = new UserInterfaceNull();
        }

        #region IRenderProvider
        public IUserInterfaceProvider UserInterface { get; private set; }

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
        public void UpdateEnvironmentalLights(IEntity pSun, IEntity pMoon) {
            return;
        }

        // Given the current mouse position, return a point in the world
        public OMV.Vector3d SelectPoint() {
            return new OMV.Vector3d(0d, 0d, 0d);
        }

        // rendering specific information for placing in  the view
        public void MapRegionIntoView(IRegionContext rcontext) {
            return;
        }

        // Set one region as the focus of display
        public void SetFocusRegion(IRegionContext rcontext) {
            return;
        }

        // something about the terrain has changed, do some updating
        public void UpdateTerrain(IRegionContext wcontext) {
            return;
        }
        #endregion IRenderProvider
    }
}
