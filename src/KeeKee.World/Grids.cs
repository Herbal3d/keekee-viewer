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
using System.IO;
using System.Text;
using KeeKee.Framework.Logging;
using KeeKee.Framework.Parameters;
using OMVSD = OpenMetaverse.StructuredData;

namespace KeeKee.World {
/// <summary>
/// Keeps a list of the possible grids and returns info as requested
/// </summary>
public class Grids {

    public readonly static string Current = "CURRENT";

    private ParameterSet m_gridInfo = null;
    private string m_currentGrid = "UnknownXXYYZZ";

    public Grids() {
        KeeKeeBase.Instance.AppParams.AddDefaultParameter("Grids.Filename.Directory",
            Utilities.GetDefaultApplicationStorageDir(null),
            "Directory that should contain the grid filename");
        KeeKeeBase.Instance.AppParams.AddDefaultParameter("Grids.Filename", "Grids.json",
            "Filename of grid specs");
    }

    // cause the grid information to be reloaded
    public void Reload() {
        m_gridInfo = null;
    }

    // set the grid name so Grids.Current works
    public void SetCurrentGrid(string currentGrid) {
        m_currentGrid = currentGrid;
    }

    public string GridParameter(string gridName, string parm) {
        CheckInit();
        string ret = null;
        string lookupGrid = gridName;
        if (gridName == "CURRENT") lookupGrid = m_currentGrid;
        try {
            if (m_gridInfo.HasParameter(lookupGrid)) {
                OMVSD.OSDMap gInfo = (OMVSD.OSDMap)m_gridInfo.ParamValue(lookupGrid);
                ret = gInfo[parm].AsString();
            }
        }
        catch {
            ret = null;
        }
        return ret;
    }

    public string GridLoginURI(string gridName) {
        CheckInit();
        string ret = null;
        string lookupGrid = gridName;
        if (gridName == "CURRENT") lookupGrid = m_currentGrid;
        try {
            if (m_gridInfo.HasParameter(lookupGrid)) {
                OMVSD.OSDMap gInfo = (OMVSD.OSDMap)m_gridInfo.ParamValue(lookupGrid);
                ret = gInfo["LoginURL"].AsString();
            }
        }
        catch {
            ret = null;
        }
        return ret;
    }

    // Performs an action on each map which describes a grid ("Name", "LoginURL", ...)
    public void ForEach(Action<OMVSD.OSDMap> act) {
        CheckInit();
        try {
            m_gridInfo.ForEach(delegate(string k, OMVSD.OSD v) {
                act((OMVSD.OSDMap)v);
            });
        }
        catch (Exception e) {
            LogManager.Log.Log(LogLevel.DBADERROR, "GridList.ForEach: Exception: {0}", e.ToString());
        }
    }
    
    // see that the grid info is read in. Called at the beginning of every data access method
    private void CheckInit() {
        if (m_gridInfo == null) {
            string gridsFilename = "";
            try {
                m_gridInfo = new ParameterSet();
                gridsFilename = Path.Combine(KeeKeeBase.Instance.AppParams.ParamString("Grids.Filename.Directory"),
                                    KeeKeeBase.Instance.AppParams.ParamString("Grids.Filename"));
                if (!File.Exists(gridsFilename)) {
                    // if the user copy of the config file doesn't exist, copy the default into place
                    string gridsDefaultFilename = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, 
                                    KeeKeeBase.Instance.AppParams.ParamString("Grids.Filename"));
                    if (File.Exists(gridsDefaultFilename)) {
                        File.Copy(gridsDefaultFilename, gridsFilename);
                    }
                    else {
                        LogManager.Log.Log(LogLevel.DBADERROR, "GridManager: GRIDS FILE DOES NOT EXIST: {0}", gridsFilename);
                        gridsFilename = null;
                    }
                }
                if (gridsFilename != null) {
                    m_gridInfo.AddFromFile(gridsFilename);
                }
            }
            catch (Exception e) {
                LogManager.Log.Log(LogLevel.DBADERROR, "GridManager: FAILED READING GRIDS FILE '{0}': {1}",
                        gridsFilename, e.ToString());

            }
        }
    }

}
}
