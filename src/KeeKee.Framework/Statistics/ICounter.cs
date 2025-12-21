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

namespace KeeKee.Framework.Statistics {
    /// <summary>
    /// Counters have names. Someone wishing to create a counter calls the
    /// StatisticsManager to get one, then one calls In() to get a
    /// starting value and then calls Out() with that value when the work
    /// is done. The passing of the value helps keep threads from stepping
    /// on each other.
    /// </summary>
    
    // used if counter value is kept externally
    public delegate long CounterValueCallback();

    public interface ICounter {
        string Name { get; }

        void Event();       // count an event

        long Count { get; }  // total number of events
    }
}
