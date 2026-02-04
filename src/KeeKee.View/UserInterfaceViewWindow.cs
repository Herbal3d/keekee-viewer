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

using KeeKee.Contexts;
using KeeKee.Framework.Logging;
using KeeKee.Framework.WorkQueue;
using KeeKee.Renderer;

namespace KeeKee.View {

    public class UserInterfaceViewWindow : IUserInterfaceProvider {

        private KLogger<UserInterfaceViewWindow> m_log;

#pragma warning disable 0067   // disable unused event warning
        public event UserInterfaceKeypressCallback OnUserInterfaceKeypress;
        public event UserInterfaceMouseMoveCallback OnUserInterfaceMouseMove;
        public event UserInterfaceMouseButtonCallback OnUserInterfaceMouseButton;
        public event UserInterfaceEntitySelectedCallback OnUserInterfaceEntitySelected;
#pragma warning restore 0067

        IUserInterfaceProvider m_ui;

        public UserInterfaceViewWindow(KLogger<UserInterfaceViewWindow> pLog,
                                       // IOptions<UserInterfaceConfig> pConfig,
                                       WorkQueueManager pQueueManager) {
            m_log = pLog;

            m_ui = new UserInterfaceCommon(pLog, pQueueManager);
            m_ui.OnUserInterfaceKeypress += UI_OnUserInterfaceKeypress;
            m_ui.OnUserInterfaceMouseMove += UI_OnUserInterfaceMouseMove;
            m_ui.OnUserInterfaceMouseButton += UI_OnUserInterfaceMouseButton;
            m_ui.OnUserInterfaceEntitySelected += UI_OnUserInterfaceEntitySelected;
        }

        private void UI_OnUserInterfaceKeypress(Keys key, bool updown) {
            if (OnUserInterfaceKeypress != null) OnUserInterfaceKeypress(key, updown);
        }
        private void UI_OnUserInterfaceMouseMove(int parm, float x, float y) {
            if (OnUserInterfaceMouseMove != null) OnUserInterfaceMouseMove(parm, x, y);
        }
        private void UI_OnUserInterfaceMouseButton(MouseButtons mbut, bool updown) {
            if (OnUserInterfaceMouseButton != null) OnUserInterfaceMouseButton(mbut, updown);
        }
        private void UI_OnUserInterfaceEntitySelected(IEntity ent) {
            if (OnUserInterfaceEntitySelected != null) OnUserInterfaceEntitySelected(ent);
        }

        #region IUserInterfaceProvider
        // IUserInterfaceProvider.InputModeCode
        public InputModeCode InputMode { get { return m_ui.InputMode; } set { m_ui.InputMode = value; } }

        // IUserInterfaceProvider.LastKeyCode
        public Keys LastKeyCode { get { return m_ui.LastKeyCode; } set { m_ui.LastKeyCode = value; } }

        // IUserInterfaceProvider.KeyPressed
        public bool KeyPressed { get { return m_ui.KeyPressed; } set { m_ui.KeyPressed = value; } }

        // IUserInterfaceProvider.LastMouseButtons
        public MouseButtons LastMouseButtons { get { return m_ui.LastMouseButtons; } set { m_ui.LastMouseButtons = value; } }

        // IUserInterfaceProvider.KeyRepeatRate
        public float KeyRepeatRate { get { return m_ui.KeyRepeatRate; } set { m_ui.KeyRepeatRate = value; } }

        // IUserInterfaceProvider.ReceiveUserIO
        public void ReceiveUserIO(ReceiveUserIOInputEventTypeCode type, int param1, float param2, float param3) {
            m_ui.ReceiveUserIO(type, param1, param2, param3);
        }

        // IUserInterfaceProvider.NeedsRendererLinkage
        public bool NeedsRendererLinkage() {
            // don't hook me up with the low level stuff
            return false;
        }
        #endregion IUserInterfaceProvider

        public void Dispose() {
            if (m_ui != null) {
                m_ui.Dispose();
                m_ui = null;
            }
            return;
        }
    }
}
