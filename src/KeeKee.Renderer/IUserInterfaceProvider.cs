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
using KeeKee.World;
using System.Windows.Forms;

namespace KeeKee.Renderer {

    /// <summary>
    /// Key changed state. Fired on state change
    /// </summary>
    /// <param name="key">key code</param>
    /// <param name="updown">true if key down, false if key up</param>
    public delegate void UserInterfaceKeypressCallback(System.Windows.Forms.Keys key, bool updown);
    /// <summary>
    /// Mouse moved
    /// </summary>
    /// <param name="param">Mouse selection (only zero these days)</param>
    /// <param name="x">relative mouse movement in X direction</param>
    /// <param name="y">relative mouse movement in Y direction</param>
    public delegate void UserInterfaceMouseMoveCallback(int param, float x, float y);
    /// <summary>
    /// Mouse button changed state. This tells you about one button changing. The OR of the button
    /// states is kept in LastMouseButtons
    /// </summary>
    /// <param name="param">button codes OR'ed together</param>
    /// <param name="updown">true means down, false means went off</param>
    public delegate void UserInterfaceMouseButtonCallback(System.Windows.Forms.MouseButtons param, bool updown);
    public delegate void UserInterfaceEntitySelectedCallback(IEntity ent);

    public enum ReceiveUserIOInputEventTypeCode {
        KeyPress=1,     // p1=keycode
        KeyRelease,     // p1=keycode
        MouseMove,      // p2=x move sin last, p3=y move since last
        MouseButtonDown,// p1=button number
        MouseButtonUp,  // p1=button number
        FocusToOverlay,
        SelectEntity,
    };
    // happens to be  the same as OIS::MouseButtonID
    public enum ReceiveUserIOMouseButtonCode {
        Left = 0,
        Right,
        Middle,
        Button3,
        Button4,
        Button5,
        Button6,
        Button7
    };

    public interface IUserInterfaceProvider : IDisposable {
        // =======================================================
        // INPUT CONTROL
        event UserInterfaceKeypressCallback OnUserInterfaceKeypress;
        event UserInterfaceMouseMoveCallback OnUserInterfaceMouseMove;
        event UserInterfaceMouseButtonCallback OnUserInterfaceMouseButton;
        event UserInterfaceEntitySelectedCallback OnUserInterfaceEntitySelected;

        InputModeCode InputMode { get; set; }
        Keys LastKeyCode { get; set; }
        bool KeyPressed { get; set; }
        MouseButtons LastMouseButtons { get; set; }

        // times per second to do key repeat
        float KeyRepeatRate { get; set; }

        // process something from the input device
        void ReceiveUserIO(ReceiveUserIOInputEventTypeCode type, int param1, float param2, float param3);
        // kludge that tells the renderer that this IO system needs low level interfaces
        bool NeedsRendererLinkage();
    }
}
