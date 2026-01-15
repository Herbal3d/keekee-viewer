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

using Microsoft.Extensions.Options;

using KeeKee.Config;
using KeeKee.Framework.Logging;

using OMVSD = OpenMetaverse.StructuredData;

namespace KeeKee.Framework.WorkQueue {
    // An odd mish mash of dynamic and static. The idea is that different work
    // queues can be build (priorities, ...). For the moment, they are all
    // here using several static methods that implement the redo and scheduling.

    /// <summary>
    /// OnDemandWorkQueue is one where routines queue work for later and at some
    /// point a thread comes in and does the work. The main user is the renderer
    /// who queues work to happen between frames
    /// </summary>

    public class OnDemandWorkQueue : IWorkQueue {

        private readonly KLogger<OnDemandWorkQueue> m_log;

        private readonly WorkQueueManager m_manager;

        private CancellationToken m_cancelToken;
        private long m_totalRequests = 0;
        public long TotalQueued { get { return m_totalRequests; } }

        private string m_queueName = "";
        public string Name { get { return m_queueName; } }

        public long CurrentQueued { get { return (long)m_workQueue.Count; } }

        protected LinkedList<DoLaterJob> m_workQueue;

        public OnDemandWorkQueue(KLogger<OnDemandWorkQueue> log,
                                WorkQueueManager pManager,
                                CancellationToken pCancelToken,
                                IOptions<WorldConfig> pWorldConfig,
                                string? nam) {
            m_log = log;
            m_manager = pManager;
            m_cancelToken = pCancelToken;

            m_queueName = nam ?? "UNKNOWN";
            m_totalRequests = 0;
            m_workQueue = new LinkedList<DoLaterJob>();
        }

        public void DoLater(DoLaterJob w) {
            if (((m_totalRequests++) % 100) == 0) {
                m_log.Log(KLogLevel.DVIEWDETAIL, "{0}.DoLater: Queuing. requests={1}, queueSize={2}",
                    m_queueName, m_totalRequests, m_workQueue.Count);
            }
            w.containingClass = this;
            w.remainingWait = 0;    // the first time through, do it now
            w.timesRequeued = 0;
            AddToWorkQueue(w);
        }

        /// <summary>
        /// Experimental, untested entry which doesn't force the caller to create an
        /// instance of a DoLaterJob class but to use s delegate. The calling sequence
        /// would be something like:
        /// m_workQueue.DoLater((DoLaterCallback)delegate() { 
        ///     return LocalMethod(localParam1, localParam2, ...); 
        /// });
        /// </summary>
        /// <param name="dlcb"></param>
        public void DoLater(DoLaterCallback dlcb, Object parms) {
            this.DoLater(new DoLaterDelegateCaller(dlcb, parms));
        }

        public void DoLater(int priority, DoLaterCallback dlcb, Object parms) {
            DoLaterJob newDoer = new DoLaterDelegateCaller(dlcb, parms);
            newDoer.priority = priority;
            this.DoLater(newDoer);
        }

        private class DoLaterDelegateCaller : DoLaterJob {
            DoLaterCallback m_dlcb;
            Object m_parameters;
            public DoLaterDelegateCaller(DoLaterCallback dlcb, Object parms) : base() {
                m_dlcb = dlcb;
                m_parameters = parms;
            }
            public override bool DoIt() {
                return m_dlcb(this, m_parameters);
            }
        }

        // requeuing the work item. Since requeuing, add the delay
        public void DoLaterRequeue(ref DoLaterJob w) {
            w.timesRequeued++;
            int nextTime = Math.Min(w.requeueWait * w.timesRequeued, 5000);
            w.remainingWait = Environment.TickCount + nextTime;
            AddToWorkQueue(w);
        }

        /// <summary>
        /// Add the work item to the queue in the order order
        /// </summary>
        /// <param name="w"></param>
        private void AddToWorkQueue(DoLaterJob w) {
            lock (m_workQueue) {
                /*
                // Experimental code trying to give some order to the requests
                LinkedListNode<DoLaterJob> foundItem = null;
                for (LinkedListNode<DoLaterJob> ii = m_workQueue.First; ii != null; ii = ii.Next) {
                    if (w.order < ii.Value.order) {
                        foundItem = ii;
                        break;
                    }
                }
                if (foundItem != null) {
                    // we're pointing to an element to put our element before
                    m_workQueue.AddBefore(foundItem, w);
                }
                else {
                    // just put it on the end
                    m_workQueue.AddLast(w);
                }
                 */
                m_workQueue.AddLast(w);
            }
        }

        public void ProcessQueue() {
            ProcessQueue(50);
        }

        // A thread from the outside world calls in here to do some work on the queue
        // We process work items on the queue until the queue is empty or we reach 'maximumCost'.
        // Each queued item has a delay (a time in the future when it can be done) and a 
        // cost. As the work items are done, the cost is added up.
        // This means the thread coming in can count on being here only a limited amount
        // of time.
        public void ProcessQueue(int maximumCost) {
            int totalCost = 0;
            int totalCounter = 100;
            int now = System.Environment.TickCount;
            DoLaterJob? found = null;
            while ((totalCost < maximumCost) && (totalCounter > 0) && (m_workQueue.Count > 0)) {
                now = System.Environment.TickCount;
                found = null;
                lock (m_workQueue) {
                    // find an entry in the list who's time has come
                    foreach (DoLaterJob ww in m_workQueue) {
                        if (ww.remainingWait < now) {
                            found = ww;
                            break;
                        }
                    }
                    if (found != null) {
                        // if found, remove from list
                        m_workQueue.Remove(found);
                    }
                }
                if (found == null) {
                    // if nothing found, we're done
                    break;
                } else {
                    // try to do the operation
                    totalCounter--;
                    if (found.DoIt()) {
                        // if it worked, count it as successful
                        totalCost += found.cost;
                    } else {
                        // if it didn't work, requeue it for later
                        if (found.containingClass != null) {
                            ((OnDemandWorkQueue)found.containingClass).DoLaterRequeue(ref found);
                        }
                    }
                }
            }
        }

        public OMVSD.OSDMap GetDisplayable() {
            OMVSD.OSDMap aMap = new OMVSD.OSDMap();
            aMap.Add("Name", new OMVSD.OSDString(this.Name));
            aMap.Add("Total", new OMVSD.OSDInteger((int)this.TotalQueued));
            aMap.Add("Current", new OMVSD.OSDInteger((int)this.CurrentQueued));
            return aMap;
        }
    }
}
