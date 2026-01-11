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

namespace KeeKee.Framework.WorkQueue {
    /// <summary>
    /// An object to do later. When a work item is to be executed later, an
    /// instance is created that implements this interface and then it is
    /// enqueued in one of the queue systems. The "DoIt" method will be called
    /// sometime later -- probably when a thread is available.
    /// </summary>

    public delegate bool DoLaterCallback(DoLaterBase q, Object p);

    public abstract class DoLaterBase {
        static long baseSequence = 0;

        abstract public bool DoIt();

        public DoLaterBase() {
            requeueWait = 500;
            cost = 10;
            priority = 100;
            sequence = ++baseSequence;
        }

        public float priority;
        public int requeueWait;
        public int remainingWait;
        public int timesRequeued;
        public object? containingClass;
        public int cost;
        public long sequence;
    }
}
