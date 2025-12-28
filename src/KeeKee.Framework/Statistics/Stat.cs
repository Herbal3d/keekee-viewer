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

using OMVSD = OpenMetaverse.StructuredData;

namespace KeeKee.Framework.Statistics {

    /// <summary>
    /// Interface for a statistic that can be gathered.
    /// </summary>
    public abstract class Stat<T> : IStat {
        /// <summary>
        /// Name of the statistic.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Description of the statistic.
        /// </summary>
        public string Description { get; }

        public Action<Stat<T>>? PullAction { get; set; }

        /// <summary>
        /// Text string describing the unit of the statistic. Useful for display.
        /// </summary>
        public string Unit { get; }

        /// <summary>
        /// Internal storage of the statistic value.
        /// Most references should go through the Value property.
        /// This is uusually updated by the PullAction.
        /// </summary>
        public T? InternalValue { get; set; } = default;

        /// <summary>
        /// Get the current value of the statistic.
        /// </summary>
        /// <returns>Current value of the statistic.</returns>
        T? Value {
            get {
                // Call the pull action to update the value
                if (PullAction != null) PullAction?.Invoke(this);
                return InternalValue;
            }
        }

        /// <summary>
        /// Record an event that affects the statistic.
        /// Usually causes the statistic to update.
        /// </summary>
        public abstract void Event();
        public abstract void Event(int pCount);

        public abstract OMVSD.OSDMap GetDisplayable();

        public Stat(string pName, string pDescription, string pUnit = "") {
            Name = pName;
            Description = pDescription;
            Unit = pUnit;
        }
    }
}
