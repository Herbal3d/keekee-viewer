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

namespace KeeKee.Framework.WorkQueue {
    // A static class which keeps a list of all the allocated work queues
    // and can serve up statistics about them.
    public interface IWorkQueueManager : IDisplayable {

        // Token that indicates when the system is shutting down
        public CancellationToken ShutdownToken { get; }

        // Register and unregister work queues
        public void Register(IWorkQueue wq);
        public void Unregister(IWorkQueue wq);

        public void ForEach(Action<IWorkQueue> act);
    }
}

