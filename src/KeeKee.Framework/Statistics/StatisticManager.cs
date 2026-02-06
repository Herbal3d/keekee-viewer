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

using Microsoft.Extensions.Logging;

using OMVSD = OpenMetaverse.StructuredData;

namespace KeeKee.Framework.Statistics {
    /// <summary>
    /// Manages a group of counters and presents one REST interface to read
    /// this group of counters.
    /// </summary>
    public class StatisticManager(ILogger<StatisticManager> pLog) : IDisplayable {

        /// <summary>
        /// A statistics collection returns an OSD structure which is a map
        /// of maps. The top level map are the individual counters and
        /// their value is a map of the variables that make up the counter.
        /// </summary>
        /// <returns></returns>
        public OMVSD.OSD GetDisplayable() {
            OMVSD.OSDMap values = new OMVSD.OSDMap();
            /*
            foreach (ICounter cntr in m_counters) {
                try {
                    OMVSD.OSDMap ivals = new OMVSD.OSDMap();
                    ivals.Add("count", new OMVSD.OSDInteger((int)cntr.Count));
                    if (cntr is IIntervalCounter) {
                        IIntervalCounter icntr = (IIntervalCounter)cntr;
                        ivals.Add("average", new OMVSD.OSDInteger((int)icntr.Average));
                        ivals.Add("low", new OMVSD.OSDInteger((int)icntr.Low));
                        ivals.Add("high", new OMVSD.OSDInteger((int)icntr.High));
                        ivals.Add("last", new OMVSD.OSDInteger((int)icntr.Last));
                        ivals.Add("total", new OMVSD.OSDInteger((int)icntr.Total));
                    }
                    values.Add(cntr.Name, ivals);
                } catch (Exception e) {
                    Log.LogError("FAILURE getting Displayable value: n={0}, {1}", cntr.Name, e.ToString());
                }
            }
            */
            return values;
        }

    }
}
