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
    class IntervalCounter : StatCounter, IIntervalCounter {
        public IntervalCounter(string name) : base(name) {
        }

        // called when entering a timed region
        public int In() {
            return System.Environment.TickCount;
        }

        // called when exiting a timed region
        private Object lockThing = new Object();
        public void Out(int inValue) {
            lock (lockThing) {
                int period = System.Environment.TickCount - inValue;
                m_total += period;
                m_last = period;
                m_low = Math.Min(m_low, period);
                m_high = Math.Max(m_high, period);
                m_count++;
            }
        }

        private long m_total = 0;
        public long Total { get { return m_total; } }  // total amount of time spent (in ticks)

        private long m_last = 0;
        public long Last { get { return m_last; } }   // the length of the last period (in ticks)

        // the average period
        public long Average { 
            get {
                return m_total / m_count;
            } 
        }

        private long m_high = 0;
        public long High { get { return m_high; } }   // the largest period

        private long m_low = 0;
        public long Low { get { return m_low; } }    // the smallest period
    }
}
