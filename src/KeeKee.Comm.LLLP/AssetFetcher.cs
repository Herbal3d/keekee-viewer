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
using System.IO;
using System.Text;
using System.Threading;
using KeeKee.Framework;
using KeeKee.Framework.Logging;
using KeeKee.Framework.Statistics;
using OMV = OpenMetaverse;

namespace KeeKee.Comm.LLLP {

public delegate void AssetFetcherCompletionCallback(OMV.UUID id, string filename);

    /// <summary>
    /// WORK IN PROGRESS.
    /// This is not complete and not used anywhere yet. Someday LG will have to generalize
    /// the asset fetching. Currently, texture fetching is in LLAssetContext but, even though
    /// it is LL specific, the protocol implementation is tied to libomv. This could change.
    /// Thus, might want to pull out the protocol side of asset fetching so it can be
    /// plugged also.  This routine was a start of that and this code could either grow
    /// or be thrown out.
    /// </summary>
public class AssetFetcher {
    private ILog m_log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);

    // number of texture fetchings we set running in parallel
    public int OutStandingRequests {
        get { return m_requests.Count; }
    }

    private int m_maxParallelRequests = 5;
    public int MaxParallelRequests {
        get { return m_maxParallelRequests; }
        set { m_maxParallelRequests = value; }
    }

    public StatisticManager m_stats;
    public ICounter m_totalRequests;         // count of total requests
    public ICounter m_duplicateRequests;     // count of requests for things we're already queued for
    public ICounter m_requestsForExisting;   // count of requests for assets that are already in files

    struct TRequest {
        public OMV.UUID ID;
        public string Filename;
        public int QueueTime;
        // public int RequestTime;
        public OMV.AssetType Type;
        public AssetFetcherCompletionCallback DoneCall;
    };

    private Dictionary<string, TRequest> m_requests;
    private List<TRequest> m_outstandingRequests;
    private OMV.GridClient m_client;

    public AssetFetcher(OMV.GridClient grid) {
        m_client = grid;
        // m_client.Assets.OnAssetReceived += new OMV.AssetManager.AssetReceivedCallback(Assets_OnAssetReceived);
        m_requests = new Dictionary<string, TRequest>();
        m_outstandingRequests = new List<TRequest>();
        m_stats = new StatisticManager("AssetFetcher");
        m_totalRequests = m_stats.GetCounter("TotalRequests");
        m_duplicateRequests = m_stats.GetCounter("DuplicateRequests");
        m_requestsForExisting = m_stats.GetCounter("RequestsForExistingAsset");
    }

    public void AssetIntoFile(OMV.UUID getID, OMV.AssetType type, string filename, AssetFetcherCompletionCallback doneCall) {
        m_totalRequests.Event();
        if (File.Exists(filename)) {
            m_requestsForExisting.Event();
            // doneCall.BeginInvoke(getID, filename, null, null);
            ThreadPool.QueueUserWorkItem((WaitCallback)delegate(Object x) {
            // ThreadPool.UnsafeQueueUserWorkItem((WaitCallback)delegate(Object x) {
                doneCall(getID, filename);
            }, null);


        }
        lock (m_requests) {
            if (!m_requests.ContainsKey(filename)) {
                TRequest treq = new TRequest();
                treq.ID = getID;
                treq.Filename = filename;
                treq.Type = type;
                treq.DoneCall = doneCall;
                treq.QueueTime = System.Environment.TickCount;
                m_requests.Add(filename, treq);
            }
            else {
                m_duplicateRequests.Event();
            }
        }
        PushRequests();
    }

    private void PushRequests() {
        lock (m_requests) {
            if ((m_outstandingRequests.Count < m_maxParallelRequests) && (m_requests.Count > 0)) {
                // there is room for more requests
                // TODO: Move some requests from m_requests to m_outstandingRequests and start the request
            }
        }
    }

    private void Assets_OnAssetReceived() {
    }
}
}
