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
using System.Threading;
using KeeKee.Comm;
using KeeKee.Framework;
using KeeKee.Framework.Logging;
using KeeKee.World;
using KeeKee.World.LL;
using OMV = OpenMetaverse;

namespace KeeKee.Comm.LLLP {
    /// <summary>
    /// If we get started up after OpenMetaverse as been logged in, we must
    /// suck the state out of the OpenMetaverse library and push it into
    /// our world representation.
    /// </summary>
public class LoadWorldObjects {
    static LoadWorldObjects() {
    }

    public static void Load(OMV.GridClient netComm, CommLLLP worldComm) {
        LogManager.Log.Log(LogLevel.DCOMM, "LoadWorldObjects: loading existing context");
        List<OMV.Simulator> simsToLoad = new List<OMV.Simulator>();
        lock (netComm.Network.Simulators) {
            foreach (OMV.Simulator sim in netComm.Network.Simulators) {
                if (WeDontKnowAboutThisSimulator(sim, netComm, worldComm)) {
                    // tell the world about this simulator
                    LogManager.Log.Log(LogLevel.DCOMMDETAIL, "LoadWorldObjects: adding simulator {0}", sim.Name);
                    worldComm.Network_SimConnected(netComm, new OMV.SimConnectedEventArgs(sim));
                    simsToLoad.Add(sim);
                }
            }
        }
        Object[] loadParams = { simsToLoad, netComm, worldComm };
        ThreadPool.QueueUserWorkItem(LoadSims, loadParams);
        // ThreadPool.UnsafeQueueUserWorkItem(LoadSims, loadParams);
        LogManager.Log.Log(LogLevel.DCOMM, "LoadWorldObjects: started thread to load sim objects");
    }

    /// <summary>
    /// Routine called on a separate thread to load the avatars and objects from the simulators
    /// into KeeKee.
    /// </summary>
    /// <param name="loadParam"></param>
    private static void LoadSims(Object loadParam) {
        LogManager.Log.Log(LogLevel.DCOMM, "LoadWorldObjects: starting to load sim objects");
        try {
            Object[] loadParams = (Object[])loadParam;
            List<OMV.Simulator> simsToLoad = (List<OMV.Simulator>)loadParams[0];
            OMV.GridClient netComm = (OMV.GridClient)loadParams[1];
            CommLLLP worldComm = (CommLLLP)loadParams[2];

            OMV.Simulator simm = null;
            try {
                foreach (OMV.Simulator sim in simsToLoad) {
                    simm = sim;
                    LoadASim(sim, netComm, worldComm);
                }
            }
            catch (Exception e) {
                LogManager.Log.Log(LogLevel.DBADERROR, "LoadWorldObjects: exception loading {0}: {1}",
                    (simm == null ? "NULL" : simm.Name), e.ToString());
            }
        }
        catch (Exception e) {
            LogManager.Log.Log(LogLevel.DBADERROR, "LoadWorldObjects: exception: {0}", e.ToString());
        }
        LogManager.Log.Log(LogLevel.DCOMM, "LoadWorldObjects: completed loading sim objects");
    }

    public static void LoadASim(OMV.Simulator sim, OMV.GridClient netComm, CommLLLP worldComm) {
        LogManager.Log.Log(LogLevel.DCOMM, "LoadWorldObjects: loading avatars and objects for sim {0}", sim.Name);
        AddAvatars(sim, netComm, worldComm);
        AddObjects(sim, netComm, worldComm);
    }

    // Return 'true' if we don't have this region in our world yet
    private static bool WeDontKnowAboutThisSimulator(OMV.Simulator sim, OMV.GridClient netComm, CommLLLP worldComm) {
        LLRegionContext regn = worldComm.FindRegion(delegate(LLRegionContext rgn) {
            return rgn.Simulator.ID == sim.ID;
        });
        return (regn == null);
    }

    private static void AddAvatars(OMV.Simulator sim, OMV.GridClient netComm, CommLLLP worldComm) {
        LogManager.Log.Log(LogLevel.DCOMM, "LoadWorldObjects: loading {0} avatars", sim.ObjectsAvatars.Count);
        List<OMV.Avatar> avatarsToNew = new List<OpenMetaverse.Avatar>();
        sim.ObjectsAvatars.ForEach(delegate(OMV.Avatar av) {
            avatarsToNew.Add(av);
        });
        // this happens outside the avatar list lock
        foreach (OMV.Avatar av in avatarsToNew) {
            worldComm.Objects_AvatarUpdate(netComm, new OMV.AvatarUpdateEventArgs(sim, av, 0, true));
        }
    }

    private static void AddObjects(OMV.Simulator sim, OMV.GridClient netComm, CommLLLP worldComm) {
        LogManager.Log.Log(LogLevel.DCOMM, "LoadWorldObjects: loading {0} primitives", sim.ObjectsPrimitives.Count);
        List<OMV.Primitive> primsToNew = new List<OpenMetaverse.Primitive>();
        sim.ObjectsPrimitives.ForEach(delegate(OMV.Primitive prim) {
            primsToNew.Add(prim);
        });
        foreach (OMV.Primitive prim in primsToNew) {
            // TODO: how can we tell if this prim might be an attachment?
            worldComm.Objects_ObjectUpdate(netComm, new OpenMetaverse.PrimEventArgs(sim, prim, 0, true, false));
        }
    }




}
}
