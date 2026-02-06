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

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using KeeKee.Config;
using KeeKee.Framework.Logging;

using OMVSD = OpenMetaverse.StructuredData;

namespace KeeKee.Framework.WorkQueue {
    // A static class which keeps a list of all the allocated work queues
    // and can serve up statistics about them.
    public class WorkQueueManager : BackgroundService, IDisplayable {
        private readonly KLogger<WorkQueueManager> m_log;

        private List<IWorkQueue> m_queues;

        private IServiceProvider m_provider;
        public CancellationToken ShutdownToken { get; private set; }

        public WorkQueueManager(KLogger<WorkQueueManager> pLog,
                                IServiceProvider pProvider) {
            m_log = pLog;
            m_provider = pProvider;
            m_queues = new List<IWorkQueue>();
        }
        protected override async Task ExecuteAsync(CancellationToken cancellationToken) {
            m_log.LogInfo("WorkQueueManager starting.");

            ShutdownToken = cancellationToken;

            await Task.CompletedTask;
        }

        // Create and return a BasicWorkQueue registered with this manager
        public BasicWorkQueue CreateBasicWorkQueue(string name) {
            var q = new BasicWorkQueue(
                m_provider.GetRequiredService<KLogger<BasicWorkQueue>>(),
                this,
                m_provider.GetRequiredService<IOptions<WorldConfig>>(),
                name
                );
            Register(q);
            return q;
        }

        public void Register(IWorkQueue wq) {
            m_log.Log(KLogLevel.DINITDETAIL, "registering queue {0}", wq.Name);
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

        /// <summary>
        /// Return an OSDArray of  the collection of work queues and their statistics
        /// </summary>
        /// <returns></returns>
        public OMVSD.OSD GetDisplayable() {
            OMVSD.OSDArray aMap = new OMVSD.OSDArray();
            lock (m_queues) {
                foreach (IWorkQueue wq in m_queues) {
                    aMap.Add(wq.GetDisplayable());
                }
            }
            return aMap;
        }
    }
}
