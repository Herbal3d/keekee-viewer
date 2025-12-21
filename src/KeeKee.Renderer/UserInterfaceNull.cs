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
using System.Windows.Forms;

namespace KeeKee.Renderer {

    /// <summary>
    /// This provides the necessary callbacks to keep the renderer and viewer
    /// happy while not really doing any user IO. Used when in Radegast mode
    /// when all the UI is provided by Radegast.
    /// </summary>
public class UserInterfaceNull : IUserInterfaceProvider {

# pragma warning disable 0067   // disable unused event warning
    public event UserInterfaceKeypressCallback OnUserInterfaceKeypress;
    public event UserInterfaceMouseMoveCallback OnUserInterfaceMouseMove;
    public event UserInterfaceMouseButtonCallback OnUserInterfaceMouseButton;
    public event UserInterfaceEntitySelectedCallback OnUserInterfaceEntitySelected;
# pragma warning restore 0067

    // IUserInterfaceProvider.InputModeCode
    private InputModeCode m_inputMode;
    public InputModeCode InputMode { 
        get { return m_inputMode; }
        set { m_inputMode = value; }
    }

    // IUserInterfaceProvider.LastKeyCode
    private Keys m_lastKeycode = 0;
    public Keys LastKeyCode {
        get { return m_lastKeycode; }
        set { m_lastKeycode = value; }
    }

    // IUserInterfaceProvider.KeyPressed
    private bool m_keyPressed = false;
    public bool KeyPressed {
        get { return m_keyPressed; }
        set { m_keyPressed = value; }
    }

    // IUserInterfaceProvider.LastMouseButtons
    private MouseButtons m_lastButtons = 0;
    public MouseButtons LastMouseButtons {
        get { return m_lastButtons; }
        set { m_lastButtons = value; }
    }

    // IUserInterfaceProvider.KeyRepeatRate
    private float m_repeatRate = 3f;
    public float KeyRepeatRate {
        get { return m_repeatRate; }
        set { m_repeatRate = value; }
    }

    // IUserInterfaceProvider.ReceiveUserIO
    public void ReceiveUserIO(ReceiveUserIOInputEventTypeCode type, int param1, float param2, float param3) {
        return;
    }

    // IUserInterfaceProvider.NeedsRendererLinkage
    public bool NeedsRendererLinkage() {
        // don't hook me up with the low level stuff
        return false;
    }

    public void Dispose() {
        return;
    }
}
}
