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

using KeeKee.Framework.WorkQueue;

namespace KeeKee.World {

    public enum RegionStateCode : uint {
        None = 0,
        Uninitialized = 1 << 0,
        Connected = 1 << 1, // we've heard about the region but have not received 'connected' message
        Online = 1 << 2, // fully connected and running
        Disconnected = 1 << 3, // we lost the connection. It's here but we can't talk to it
        LowRez = 1 << 4, // a disconnected region that's here as a low rez representation
        ShuttingDown = 1 << 5, // region is shutting down
        Down = 1 << 6, // disconnected and probably getting freed
        Focus = 1 << 7, // this is the focus region (with the agent present)
    }

    public delegate void RegionStateChangedCallback(RegionStateCode code);
    public delegate void RegionStateCheckCallback();

    public class RegionState {
        public event RegionStateChangedCallback? OnStateChanged;

        // one work queue for all the state update work
        private static BasicWorkQueue m_stateWork;

        private RegionStateCode m_regionState = RegionStateCode.Uninitialized;
        private Object m_regionStateLock = new Object();

        public RegionStateCode State {
            get { return m_regionState; }
            set {
                RegionStateCode newState = value;
                lock (m_regionStateLock) {
                    if (m_regionState != newState) {
                        m_regionState = newState;
                        // if (OnStateChanged != null) OnStateChanged(m_regionState);
                        if (OnStateChanged != null) {
                            // queue the state changed event to happen on another thread
                            m_stateWork.DoLater(new OnStateChangedLater(OnStateChanged, m_regionState));
                        }
                    }
                }
            }
        }

        private class OnStateChangedLater : DoLaterJob {
            RegionStateChangedCallback m_callback;
            RegionStateCode m_code;
            public OnStateChangedLater(RegionStateChangedCallback c, RegionStateCode r) {
                m_callback = c;
                m_code = r;
            }
            public override bool DoIt() {
                m_callback(m_code);
                return true;
            }
        }

        public RegionState(WorkQueueManager pQueueManager) {
            m_stateWork = pQueueManager.CreateBasicWorkQueue("RegionStateWorkQueue");
            m_regionState = RegionStateCode.Uninitialized;
        }

        /// <summary>
        /// Return 'true' if the region is online, running and fully usable.
        /// If 'false' is returned, the region is either being initialized (after
        /// being created but before we're received the 'connected' message)
        /// or is being shutdown.
        /// </summary>
        public bool isOnline {
            get { return ((m_regionState & (RegionStateCode.Online)) != 0); }
        }

        // Will perform the callback if we're not online. The callback is done while
        // the state is locked thus preventing race conditions.
        // Returns 'true' if the region is not online and we called the delegate
        public bool IfNotOnline(RegionStateCheckCallback rscc) {
            bool ret = false;
            lock (m_regionStateLock) {
                if (!isOnline) {
                    rscc();
                    ret = true;
                }
            }
            return ret;
        }
        // Will perform the callback if we're online. The callback is done while
        // the state is locked thus preventing race conditions.
        // Returns 'true' if we called the delegate
        public bool IfOnline(RegionStateCheckCallback rscc) {
            bool ret = false;
            lock (m_regionStateLock) {
                if (isOnline) {
                    rscc();
                    ret = true;
                }
            }
            return ret;
        }
    }
}
