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

using KeeKee.Config;
using KeeKee.Framework.Logging;

using OMVSD = OpenMetaverse.StructuredData;
using Microsoft.Extensions.Options;

namespace KeeKee.Framework.WorkQueue {
    public class BasicWorkQueue : IWorkQueue {
        private readonly KLogger<BasicWorkQueue> m_log;

        private readonly WorkQueueManager m_manager;

        private CancellationToken m_cancelToken;

        // IWorkQueue.TotalQueued()
        private long m_totalRequests = 0;
        public long TotalQueued { get { return m_totalRequests; } }

        // IWorkQueue.Name()
        public string Name { get; set; } = "UNKNOWN";

        private Queue<DoLaterJob> m_workItems;

        public int ActiveWorkProcessors { get; set; }
        public int MaxWorkProcessors { get; set; } = 4;
        // IWorkQueue.CurrentQueued()
        public long CurrentQueued { get { return (long)m_workItems.Count; } }

        public BasicWorkQueue(KLogger<BasicWorkQueue> log,
                                WorkQueueManager pManager,
                                IOptions<WorldConfig> pWorldConfig,
                                string pName) {

            m_log = log;
            m_manager = pManager;
            m_cancelToken = m_manager.ShutdownToken;
            Name = pName;

            m_workItems = new Queue<DoLaterJob>();
            m_totalRequests = 0;
            m_manager.Register(this);

            MaxWorkProcessors = pWorldConfig.Value.MaxWorkQueueItems;

            // Start up the task that keeps the work items working
            lock (m_doEvenLater) {
                if (m_doEvenLaterTask == null) {
                    m_doEvenLaterTask = Task.Run(DoItEventLaterProcessing, m_cancelToken);

                    m_log.Log(KLogLevel.DINIT, "Starting do even later task for '{0}'", Name);
                }
            }
        }

        // IWorkQueue.DoLater()
        public void DoLater(DoLaterJob w) {
            w.containingClass = this;
            w.remainingWait = 0;    // the first time through, do it now
            w.timesRequeued = 0;
            AddWorkItemToQueue(w);
        }

        // Doing the work didn't work the first time so we again add it to the queue
        public void DoLaterRequeue(DoLaterJob w) {
            AddWorkItemToQueue(w);
        }

        /// <summary>
        /// Entry which doesn't force the caller to create an
        /// instance of a DoLaterJob class but to use a delegate. The calling sequence
        /// would be something like:
        /// <pre>
        ///     Object[] parms = { localParam1, localParam2, ...};
        ///     m_workQueue.DoLater(CallbackRoutine, parms);
        /// </pre>
        /// </summary>
        /// <param name="dlcb"></param>
        public void DoLater(DoLaterCallback dlcb, Object parms) {
            this.DoLater(new DoLaterDelegateCaller(dlcb, parms));
        }

        // do the item but do  the delay first. This puts it in the wait queue and it will
        // get done after the work item delay.
        public void DoLaterInitialDelay(DoLaterCallback dlcb, Object parms) {
            DoLaterJob w = new DoLaterDelegateCaller(dlcb, parms);
            w.containingClass = this;
            w.remainingWait = 0;    // the first time through, do it now
            w.timesRequeued = 0;
            DoItEvenLater(w);

        }

        public void DoLater(float priority, DoLaterCallback dlcb, Object parms) {
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

        // Add a new work item to the work queue. If we don't have the maximum number
        // of worker threads already working on the queue, start a new thread from
        // the pool to empty the queue.
        // Worker threads come here to take items off the work queue.
        // Multiple threads loop around here taking items off the work queue and
        //   processing them. When there are no more things to do, the threads
        //   return which puts them back in the thread pool.
        private void AddWorkItemToQueue(DoLaterJob w) {
            if ((m_totalRequests++ % 100) == 0) {
                m_log.Log(KLogLevel.DRENDERDETAIL, "{0}.AddWorkItemToQueue: Queuing, c={1}, l={2}",
                                Name, m_totalRequests, m_workItems.Count);
            }
            lock (m_workItems) {
                m_workItems.Enqueue(w);
            }
            while (m_workItems.Count > 0
                    && ActiveWorkProcessors < MaxWorkProcessors
                    && m_cancelToken.IsCancellationRequested == false) {
                DoLaterJob? job = null;
                lock (m_workItems) {
                    if (m_workItems.Count > 0) {
                        job = m_workItems.Dequeue();
                        ActiveWorkProcessors++;
                    }
                }
                if (job != null) {
                    Task.Run(() => {
                        try {
                            if (!job.DoIt()) {
                                // LogManager.Log.Log(LogLevel.DRENDERDETAIL, "{0}.DoLater: DoWork: DoEvenLater", m_queueName);
                                DoItEvenLater(w);
                            }
                        } catch (Exception e) {
                            m_log.Log(KLogLevel.DBADERROR, "{0}.DoLater: DoWork: EXCEPTION: {1}",
                                        Name, e);
                            // we drop the work item in  the belief that it will exception again next time
                        }
                        lock (m_workItems) {
                            ActiveWorkProcessors--;   // not sure if this is atomic
                        }
                    }, m_cancelToken);
                }
            }
        }

        private void DoWork(object? x) {
            while (m_workItems.Count > 0) {
                DoLaterJob? w = null;
                lock (m_workItems) {
                    if (m_workItems.Count > 0) {
                        w = m_workItems.Dequeue();
                    }
                }
                if (w != null) {
                    try {
                        if (!w.DoIt()) {
                            // LogManager.Log.Log(LogLevel.DRENDERDETAIL, "{0}.DoLater: DoWork: DoEvenLater", m_queueName);
                            DoItEvenLater(w);
                        }
                    } catch (Exception e) {
                        m_log.Log(KLogLevel.DBADERROR, "{0}.DoLater: DoWork: EXCEPTION: {1}",
                                    Name, e);
                        // we drop the work item in  the belief that it will exception again next time
                    }
                }
            }
            lock (m_workItems) {
                ActiveWorkProcessors--;   // not sure if this is atomic
            }
        }

        /// <summary>
        /// Queue the operation to happen later. There is a thread who's job
        /// is waiting to run these work items.
        /// </summary>
        private List<DoLaterJob> m_doEvenLater = new List<DoLaterJob>();
        private Task? m_doEvenLaterTask = null;
        private void DoItEvenLater(DoLaterJob w) {
            w.timesRequeued++;
            lock (m_doEvenLater) {
                int nextTime = Math.Min(w.requeueWait * w.timesRequeued, 2000);
                nextTime = Math.Max(nextTime, 100);     // never less than this
                w.remainingWait = Utilities.Utilities.TickCount() + nextTime;
                m_doEvenLater.Add(w);
            }
        }

        private async Task DoItEventLaterProcessing() {
            while (m_cancelToken.IsCancellationRequested == false) {
                List<DoLaterJob> doneWaiting = new List<DoLaterJob>();
                int sleepTime = 200;
                int now = Utilities.Utilities.TickCount();
                lock (m_doEvenLater) {
                    if (m_doEvenLater.Count > 0) {
                        // remove the last waiting time from each waiter
                        // if waiting is up, remember which ones to remove
                        foreach (DoLaterJob ii in m_doEvenLater) {
                            if (ii.remainingWait < now) {
                                doneWaiting.Add(ii);
                            }
                        }
                        // remove and requeue the ones done waiting
                        if (doneWaiting.Count > 0) {
                            foreach (DoLaterJob jj in doneWaiting) {
                                m_doEvenLater.Remove(jj);
                            }
                        }
                    }
                    // m_log.Log(KLogLevel.DRENDERDETAIL, "DoEvenLater: Removing {0} from list of size {1}",
                    //             doneWaiting.Count, m_doEvenLater.Count);
                    if (m_doEvenLater.Count > 0) {
                        // find how much time to wait for the remaining
                        sleepTime = int.MaxValue;
                        foreach (DoLaterJob jj in m_doEvenLater) {
                            sleepTime = Math.Min(sleepTime, jj.remainingWait);
                        }
                        sleepTime -= now;
                        sleepTime = Math.Max(sleepTime, 100);
                        sleepTime = Math.Min(sleepTime, 3000);
                    }
                }
                // if there are some things done waiting, let them free outside the lock
                if (doneWaiting.Count > 0) {
                    foreach (DoLaterJob ll in doneWaiting) {
                        if (ll.containingClass == null) {
                            m_log.Log(KLogLevel.DBADERROR, "BasicWorkQueue.DoItEventLaterProcessing: null containingClass");
                            continue;
                        }
                        ((BasicWorkQueue)ll.containingClass).DoLaterRequeue(ll);
                    }
                    doneWaiting.Clear();
                }
                if (sleepTime > 0) {
                    await Task.Delay(sleepTime, m_cancelToken);
                }
            }
        }

        public OMVSD.OSD GetDisplayable() {
            OMVSD.OSDMap aMap = new OMVSD.OSDMap();
            aMap.Add("Name", this.Name);
            aMap.Add("Total", (int)this.TotalQueued);
            aMap.Add("Current", (int)this.CurrentQueued);
            aMap.Add("Later", m_doEvenLater.Count);
            aMap.Add("Active", ActiveWorkProcessors);
            // Logging.LogManager.Log.Log(LogLevel.DRESTDETAIL, "BasicWorkQueue: GetDisplayable: out={0}", aMap.ToString());
            return aMap;
        }
    }
}
