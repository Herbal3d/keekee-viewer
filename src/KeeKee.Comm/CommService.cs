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

using Microsoft.Extensions.Hosting;

using KeeKee.Framework.Logging;

namespace KeeKee.Comm {

    public class CommService : BackgroundService {
        private readonly ICommProvider m_comm;
        private readonly KLogger<CommService> m_log;

        public CommService(KLogger<CommService> pLog,
                              ICommProvider pComm
                              ) {
            m_log = pLog;
            m_comm = pComm;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            m_log.Log(KLogLevel.DCOMM, "Starting CommLLPService");
            await m_comm.StartAsync(stoppingToken);
            m_log.Log(KLogLevel.DCOMM, "CommLLPService started");
        }

        public override async Task StopAsync(CancellationToken cancellationToken) {
            m_log.Log(KLogLevel.DCOMM, "Stopping CommLLPService");
            await m_comm.StopAsync(cancellationToken);
            m_log.Log(KLogLevel.DCOMM, "CommLLPService stopped");
        }
    }
}
