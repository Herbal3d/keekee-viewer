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

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using KeeKee.Config;
using KeeKee.Framework.Logging;

using OMVSD = OpenMetaverse.StructuredData;

namespace KeeKee.World {
    /// <summary>
    /// Keeps a list of the possible grids and returns info as requested
    /// </summary>
    public class Grids : BackgroundService {
        private KLogger<Grids> m_log;

        private string m_currentGrid = "UnknownXXYYZZ";
        private List<IRegionContext> m_regionList;
        private IOptions<GridConfig> m_gridConfig;

        public Grids(KLogger<Grids> pLog,
                     IOptions<GridConfig> pGridConfig) {
            m_log = pLog;
            m_gridConfig = pGridConfig;

            m_regionList = new List<IRegionContext>();
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken) {
            m_log.Log(KLogLevel.DINIT, "Grid ExecuteAsync started.");
            // wait until cancelled
            try {
                while (!cancellationToken.IsCancellationRequested) {
                    await Task.Delay(1000, cancellationToken);
                }
            } catch (TaskCanceledException) {
                // normal exit path
            }
            m_log.Log(KLogLevel.DINIT, "Grid ExecuteAsync exiting.");
        }


        // set the grid name so Grids.Current works
        public void SetCurrentGrid(string currentGrid) {
            m_currentGrid = currentGrid;
        }

        public string? GridLoginURI(string gridName) {
            string ret = m_gridConfig.Value.Grids[gridName].LoginURI;
            return ret;
        }

        // Performs an action on each map which describes a grid ("Name", "LoginURL", ...)
        public void ForEach(Action<GridConfig.GridDefinition> act) {
            try {
                foreach (var kvp in m_gridConfig.Value.Grids) {
                    act(kvp.Value);
                }
                ;
            } catch (Exception e) {
                m_log.Log(KLogLevel.DBADERROR, "GridList.ForEach: Exception: {0}", e.ToString());
            }
        }
    }
}
