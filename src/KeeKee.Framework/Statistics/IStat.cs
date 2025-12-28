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
    /// Top level interface for a statistic that can be gathered.
    /// </summary>
    public interface IStat : IDisplayable {
        string Name { get; }
        string Description { get; }
        string Unit { get; }

        void Event();
        void Event(int pCount);
    }

}
