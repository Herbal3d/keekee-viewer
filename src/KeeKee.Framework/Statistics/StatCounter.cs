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

namespace KeeKee.Framework.Statistics {
    public class StatCounter : ICounter {
        public StatCounter(string name) {
            m_name = name;
        }

        public void Event() {
            m_count++;
        }

        protected string m_name;
        public string Name { get { return m_name; } }

        protected long m_count = 1;     // starts at one to elimiate any divide by zeros
        public long Count { get { return m_count; } }  // total number of In/Out calls
    }
}
