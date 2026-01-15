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

namespace KeeKee.Renderer {

    /// <summary>
    /// This provides the necessary callbacks to keep the renderer and viewer
    /// happy while not really doing any user IO. Used when in Radegast mode
    /// when all the UI is provided by Radegast.
    /// </summary>
    public class UserInterfaceNull : IUserInterfaceProvider {

        public event UserInterfaceKeypressCallback? OnUserInterfaceKeypress;
        public event UserInterfaceMouseMoveCallback? OnUserInterfaceMouseMove;
        public event UserInterfaceMouseButtonCallback? OnUserInterfaceMouseButton;
        public event UserInterfaceEntitySelectedCallback? OnUserInterfaceEntitySelected;

        // IUserInterfaceProvider.InputModeCode
        public InputModeCode InputMode { get; set; }

        // IUserInterfaceProvider.LastKeyCode
        public Keys LastKeyCode { get; set; }

        // IUserInterfaceProvider.KeyPressed
        public bool KeyPressed { get; set; } = false;

        // IUserInterfaceProvider.LastMouseButtons
        public MouseButtons LastMouseButtons { get; set; } = 0;

        // IUserInterfaceProvider.KeyRepeatRate
        public float KeyRepeatRate { get; set; } = 3f;

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
