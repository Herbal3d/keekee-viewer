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
using KeeKee.Framework.Logging;
using KeeKee.Framework.Statistics;
using OMVSD = OpenMetaverse.StructuredData;

namespace KeeKee.Framework.WorkQueue {
    // A static class which keeps a list of all the allocated work queues
    // and can serve up statistics about them.
    public class WorkQueueManager : IDisplayable , IInstance<WorkQueueManager> {

        private List<IWorkQueue> m_queues;

        private static WorkQueueManager m_instance = null;
        public static WorkQueueManager Instance {
            get {
                if (m_instance == null) m_instance = new WorkQueueManager();
                return m_instance;
            }
        }
        
        public WorkQueueManager() {
            m_queues = new List<IWorkQueue>();
        }

        public void Register(IWorkQueue wq) {
            Logging.LogManager.Log.Log(LogLevel.DINITDETAIL, "WorkQueueManager: registering queue {0}", wq.Name);
            lock (m_queues) m_queues.Add(wq);
        }

        public void Unregister(IWorkQueue wq) {
            lock (m_queues) m_queues.Remove(wq);
        }

        public void ForEach(Action<IWorkQueue> act) {
            lock (m_queues) {
                foreach (IWorkQueue wq in m_queues) {
                    act(wq);
                }
            }
        }

        public OMVSD.OSDMap GetDisplayable() {
            OMVSD.OSDMap aMap = new OMVSD.OSDMap();
            lock (m_queues) {
                foreach (IWorkQueue wq in m_queues) {
                    try {
                        aMap.Add(wq.Name, wq.GetDisplayable());
                    }
                    catch {
                        LogManager.Log.Log(LogLevel.DBADERROR, "WorkQueueManager.GetDisplayable: duplicate symbol: {0}", wq.Name);
                    }
                }
            }
            return aMap;
        }
    }
}
