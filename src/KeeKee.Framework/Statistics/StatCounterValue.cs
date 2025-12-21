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
    public class StatCounterValue : ICounter {
        public StatCounterValue(string name, CounterValueCallback valueCall) {
            m_name = name;
            m_valueCall = valueCall;
        }

        public void Event() {
            // the event is not used for this type of value: NOOP
        }

        protected string m_name;
        public string Name { get { return m_name; } }

        CounterValueCallback m_valueCall;
        public long Count { get { return m_valueCall(); } }
    }
}
