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
using KeeKee;

namespace KeeKee.Framework.WorkQueue {
    public delegate bool DoLaterCall(Object ob);

    public interface IWorkQueue : IDisplayable {

        // queue work to do later
        void DoLater(DoLaterJob x);

        // return the total number amount of work ever queued
        long TotalQueued { get; }

        // return the amount of work queued
        long CurrentQueued { get; }

        // work queues have names so we can track them
        string Name { get; }

    }
}
