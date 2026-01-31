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

using KeeKee.Framework.Logging;
using KeeKee.Framework.WorkQueue;

namespace KeeKee.Renderer {

    public class UserInterfaceCommon : IUserInterfaceProvider {
        private IKLogger m_log;

        public event UserInterfaceKeypressCallback? OnUserInterfaceKeypress;
        public event UserInterfaceMouseMoveCallback? OnUserInterfaceMouseMove;
        public event UserInterfaceMouseButtonCallback? OnUserInterfaceMouseButton;
        public event UserInterfaceEntitySelectedCallback? OnUserInterfaceEntitySelected;

        public InputModeCode InputMode { get; set; }

        /// <summary>
        ///  Remember the last key code we returned. Mostly to remember which modifier
        ///  keys are on.
        /// </summary>
        public Keys LastKeyCode { get; set; }

        /// <summary>
        ///  Whether a key is up or pressed at the moment.
        /// </summary>
        public bool KeyPressed { get; set; } = false;

        /// <summary>
        /// Remember the last (current) mouse button positions for easy checking
        /// </summary>
        public MouseButtons LastMouseButtons { get; set; } = 0;

        /// <summary>
        /// The rate to repeat the keys (repeats per second). Zero says no repeat
        /// </summary>
        private float m_keyRepeatRate = 3f;
        public float KeyRepeatRate {
            get { return m_keyRepeatRate; }
            set {
                m_keyRepeatRate = value;
                m_keyRepeatMs = (int)(1000f / m_keyRepeatRate);
            }
        }
        private int m_keyRepeatMs = 333;
        public int KeyRepeatMs { get { return m_keyRepeatMs; } }
        public Timer m_repeatTimer;
        public int m_repeatKey;    // the raw key code that is being repeated

        private BasicWorkQueue m_workQueue;

        public UserInterfaceCommon(IKLogger pLog,
                                   WorkQueueManager pQueueManager) {
            m_log = pLog;
            m_workQueue = pQueueManager.CreateBasicWorkQueue("UICommonWorkQueue");
            m_repeatTimer = new Timer(OnRepeatTimer);
        }

        public void Dispose() {
            if (m_repeatTimer != null) {
                m_repeatTimer.Dispose();
            }
        }

        // I need the hooks to the lowest levels
        public bool NeedsRendererLinkage() {
            return false;
        }

        // If the key is still held down, fake a key press
        private void OnRepeatTimer(Object? xx) {
            if (this.KeyPressed) {
                // fake receiving another key press
                if (m_workQueue.CurrentQueued < 4) { // if getting behind, don't repeat
                    ReceiveUserIO(ReceiveUserIOInputEventTypeCode.KeyPress, m_repeatKey, 0f, 0f);
                    // m_log.Log(LogLevel.DBADERROR, "OnRepeatTimer: Faking key {0}", m_repeatKey);
                }
            } else {
                // key not pressed so don't repeat any more
                m_repeatTimer.Change(Timeout.Infinite, Timeout.Infinite);
                // m_log.Log(LogLevel.DBADERROR, "OnRepeatTimer: Disabling timer");
            }
        }

        // IUserInterfaceProvider.ReceiveUserIO
        /// <summary>
        /// One of the input subsystems has received a charaaction or mouse. Queue the
        /// processing to delink us from the IO thread.
        /// If a typed char, we use it to update the modifiers (alt, ...) and  then
        /// assemble the keycode or'ed with the current modifers into this.LastKeyCode.
        /// Then, anyone waiting is given a callback.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="param1"></param>
        /// <param name="param2"></param>
        /// <param name="param3"></param>
        public void ReceiveUserIO(ReceiveUserIOInputEventTypeCode type, int param1, float param2, float param3) {
            Object[] receiveLaterParams = { type, param1, param2, param3 };
            m_workQueue.DoLater(ReceiveLater, receiveLaterParams);
            return;
        }

        /// <summary>
        /// One of the input subsystems has received a key or mouse press.
        /// </summary>
        /// <param name="qInstance"></param>
        /// <param name="parms"></param>
        /// <returns></returns>
        private bool ReceiveLater(DoLaterJob qInstance, Object parms) {
            Object[] loadParams = (Object[])parms;
            ReceiveUserIOInputEventTypeCode typ = (ReceiveUserIOInputEventTypeCode)loadParams[0];
            int param1 = (int)loadParams[1];
            float param2 = (float)loadParams[2];
            float param3 = (float)loadParams[3];

            switch (typ) {
                case ReceiveUserIOInputEventTypeCode.KeyPress:
                    param1 = param1 & (int)Keys.KeyCode; // remove extra cruft
                    this.UpdateModifier(param1, true);
                    AddKeyToLastKeyCode(param1);
                    m_log.Log(KLogLevel.DVIEWDETAIL, "UserInterfaceCommon: ReceiveLater: KeyPress: {0}. LastKeyCode={1}",
                                    param1, this.LastKeyCode);
                    this.m_repeatKey = param1;
                    this.KeyPressed = true;
                    this.m_repeatTimer.Change(this.KeyRepeatMs, this.KeyRepeatMs);
                    if (this.OnUserInterfaceKeypress != null)
                        this.OnUserInterfaceKeypress(this.LastKeyCode, true);
                    break;
                case ReceiveUserIOInputEventTypeCode.KeyRelease:
                    param1 = param1 & (int)Keys.KeyCode; // remove extra cruft
                    this.UpdateModifier(param1, false);
                    AddKeyToLastKeyCode(param1);
                    m_log.Log(KLogLevel.DVIEWDETAIL, "UserInterfaceCommon: ReceiveLater: KeyRelease: {0}. LastKeyCode={1}",
                                    param1, this.LastKeyCode);
                    this.KeyPressed = false;
                    this.m_repeatTimer.Change(Timeout.Infinite, Timeout.Infinite);
                    if (this.OnUserInterfaceKeypress != null)
                        this.OnUserInterfaceKeypress(this.LastKeyCode, false);
                    break;
                case ReceiveUserIOInputEventTypeCode.MouseButtonDown:
                    this.UpdateMouseModifier(param1, true);
                    m_log.Log(KLogLevel.DVIEWDETAIL, "UserInterfaceCommon: ReceiveLater: MouseBtnDown: {0}, {1}",
                                    param1, this.LastMouseButtons);
                    if (this.OnUserInterfaceMouseButton != null) this.OnUserInterfaceMouseButton(
                                    ThisMouseButtonCode(param1), true);
                    break;
                case ReceiveUserIOInputEventTypeCode.MouseButtonUp:
                    this.UpdateMouseModifier(param1, false);
                    m_log.Log(KLogLevel.DVIEWDETAIL, "UserInterfaceCommon: ReceiveLater: MouseBtnUp: {0}, {1}",
                                    param1, this.LastMouseButtons);
                    if (this.OnUserInterfaceMouseButton != null) this.OnUserInterfaceMouseButton(
                                    ThisMouseButtonCode(param1), false);
                    break;
                case ReceiveUserIOInputEventTypeCode.MouseMove:
                    // pass the routine tracking the raw position information
                    // param1 is usually zero (actually mouse selector but we have only one at the moment)
                    // param2 is the X movement
                    // param3 is the Y movement
                    if (this.OnUserInterfaceMouseMove != null) this.OnUserInterfaceMouseMove(param1, param2, param3);
                    break;
            }
            // successful
            return true;
        }

        private void AddKeyToLastKeyCode(int kee) {
            this.LastKeyCode = (this.LastKeyCode & Keys.Modifiers) | (Keys)kee;
            m_log.Log(KLogLevel.DVIEWDETAIL, "UserInterfaceCommon: AddKeyToLastKeyCode: adding {0}, lkc={1}", kee, this.LastKeyCode);
        }

        /// <summary>
        /// Keep the modifier key information in a place that is easy to check later
        /// </summary>
        /// <param name="param1">OISKeyCode of the key pressed</param>
        /// <param name="updown">true if the key is down, false otherwise</param>
        private void UpdateModifier(int param1, bool updown) {
            Keys kparam1 = (Keys)param1;
            // TODO: finish this for all modifier keys
            /*
            if (kparam1 == Keys.Menu || kparam1 == Keys.LMenu || kparam1 == Keys.RMenu) {
                if (updown && ((LastKeyCode & Keys.Alt) == 0)) {
                    LastKeyCode |= Keys.Alt;
                }
                if (!updown && ((LastKeyCode & Keys.Alt) != 0)) {
                    LastKeyCode ^= Keys.Alt;
                }
            }
            if (kparam1 == Keys.ShiftKey || kparam1 == Keys.RShiftKey || kparam1 == Keys.LShiftKey) {
                if (updown && ((LastKeyCode & Keys.Shift) == 0)) {
                    LastKeyCode |= Keys.Shift;
                }
                if (!updown && ((LastKeyCode & Keys.Shift) != 0)) {
                    LastKeyCode ^= Keys.Shift;
                }
            }
            if (kparam1 == Keys.ControlKey || kparam1 == Keys.LControlKey || kparam1 == Keys.RControlKey) {
                if (updown && ((LastKeyCode & Keys.Control) == 0)) {
                    m_log.Log(KLogLevel.DVIEWDETAIL, "UserInterfaceCommon: UpdateModifier: add cntl, lkc={0}", this.LastKeyCode);
                    LastKeyCode |= Keys.Control;
                }
                if (!updown && ((LastKeyCode & Keys.Control) != 0)) {
                    m_log.Log(KLogLevel.DVIEWDETAIL, "UserInterfaceCommon: UpdateModifier: remove cntl, lkc={0}", this.LastKeyCode);
                    LastKeyCode ^= Keys.Control;
                }
            }
            */
        }

        private static MouseButtons ThisMouseButtonCode(int iosCode) {
            MouseButtons ret = MouseButtons.None;
            switch ((ReceiveUserIOMouseButtonCode)iosCode) {
                case ReceiveUserIOMouseButtonCode.Left:
                    ret = MouseButtons.Left;
                    break;
                case ReceiveUserIOMouseButtonCode.Right:
                    ret = MouseButtons.Right;
                    break;
                case ReceiveUserIOMouseButtonCode.Middle:
                    ret = MouseButtons.Middle;
                    break;
            }
            return ret;
        }

        /// <summary>
        /// Keep the mouse button state in a varaible for easy reference
        /// </summary>
        /// <param name="param1">OISKeyCode of the key pressed</param>
        /// <param name="updown">true if the key is down, false otherwise</param>
        private void UpdateMouseModifier(int param1, bool updown) {
            if (param1 == (int)ReceiveUserIOMouseButtonCode.Left) {
                if (updown) LastMouseButtons |= MouseButtons.Left;
                if (!updown && ((LastMouseButtons & MouseButtons.Left) != 0)) LastMouseButtons ^= MouseButtons.Left;
            }
            if (param1 == (int)ReceiveUserIOMouseButtonCode.Right) {
                if (updown) LastMouseButtons |= MouseButtons.Right;
                if (!updown && ((LastMouseButtons & MouseButtons.Right) != 0)) LastMouseButtons ^= MouseButtons.Right;
            }
            if (param1 == (int)ReceiveUserIOMouseButtonCode.Middle) {
                if (updown) LastMouseButtons |= MouseButtons.Middle;
                if (!updown && ((LastMouseButtons & MouseButtons.Middle) != 0)) LastMouseButtons ^= MouseButtons.Middle;
            }
        }

    }
}
