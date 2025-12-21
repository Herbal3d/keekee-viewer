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
using System.Net;
using System.Text;
using KeeKee.Framework;
using KeeKee.Framework.Logging;
using KeeKee.Framework.Modules;
using KeeKee.Framework.Parameters;
using KeeKee.Rest;
using KeeKee.World;
using KeeKee.World.LL;
using OMV = OpenMetaverse;
using OMVSD = OpenMetaverse.StructuredData;

namespace KeeKee.World.Services {

    /// <summary>
    /// Service (loaded as a module) that listens for avatars coming and going
    /// from the world and presenting a web interface of avatar presence and
    /// statistics.
    /// </summary>
public class AvatarTracker : IAvatarTrackerService, IModule {

    protected string m_moduleName;
    public string ModuleName { get { return m_moduleName; } set { m_moduleName = value; } }

    protected KeeKeeBase m_lgb = null;
    public KeeKeeBase LGB { get { return m_lgb; } }

    public IAppParameters ModuleParams { get { return m_lgb.AppParams; } }

    protected World m_world;
    protected RestHandler m_restHandler;

    protected Dictionary<string, IEntityAvatar> m_avatars;
    protected IEntityAvatar m_agentAV = null;

    public AvatarTracker() {
        // default to the class name. The module code can set it to something else later.
        m_moduleName = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name;
    }
    
    #region IModule
    // IModule.OnLoad
    public virtual void OnLoad(string modName, KeeKeeBase lgbase) {
        LogManager.Log.Log(LogLevel.DINIT, "AvatarTracker.OnLoad()");
        m_moduleName = modName;
        m_lgb = lgbase;

        m_avatars = new Dictionary<string, IEntityAvatar>();
    }

    // IModule.AfterAllModulesLoaded
    public virtual bool AfterAllModulesLoaded() {
        LogManager.Log.Log(LogLevel.DINIT, "AvatarTracker.AfterAllModulesLoaded()");
        m_restHandler = new RestHandler("/avatars", GetHandler, PostHandler);

        m_world = World.Instance;   // there is only one world
        m_world.OnAgentNew += new WorldAgentNewCallback(World_OnAgentNew);
        m_world.OnAgentRemoved += new WorldAgentRemovedCallback(World_OnAgentRemoved);
        m_world.OnWorldEntityNew += new WorldEntityNewCallback(World_OnWorldEntityNew);
        m_world.OnWorldEntityUpdate += new WorldEntityUpdateCallback(World_OnWorldEntityUpdate);
        m_world.OnWorldEntityRemoved += new WorldEntityRemovedCallback(World_OnWorldEntityRemoved);
        return true;
    }

    // IModule.Start
    public virtual void Start() {
        return;
    }

    // IModule.Stop
    public virtual void Stop() {
        // when told to stop, we forget everything
        m_avatars.Clear();
        return;
    }

    // IModule.PrepareForUnload
    public virtual bool PrepareForUnload() {
        m_world.OnAgentNew -= new WorldAgentNewCallback(World_OnAgentNew);
        m_world.OnAgentRemoved -= new WorldAgentRemovedCallback(World_OnAgentRemoved);
        m_world.OnWorldEntityNew -= new WorldEntityNewCallback(World_OnWorldEntityNew);
        m_world.OnWorldEntityRemoved -= new WorldEntityRemovedCallback(World_OnWorldEntityRemoved);
        return false;
    }

    #endregion IModule

    void World_OnAgentNew(IAgent agnt) {
        // someday remember the main agent for highlighting
        return;
    }

    void World_OnAgentRemoved(IAgent agnt) {
        return;
    }

    void World_OnWorldEntityNew(IEntity ent) {
        IEntityAvatar av;
        if (ent.TryGet<IEntityAvatar>(out av)) {
            // this new entity is an avatar. If we don't know it already, remember same
            lock (m_avatars) {
                if (!m_avatars.ContainsKey(av.Name.Name)) {
                    LogManager.Log.Log(LogLevel.DUPDATEDETAIL, "AvatarTracker. Tracking avatar {0}", av.Name.Name );
                    m_avatars.Add(av.Name.Name, av);
                }
            }
        }
        return;
    }

    void World_OnWorldEntityUpdate(IEntity ent, UpdateCodes what) {
        IEntityAvatar av;
        if (ent.TryGet<IEntityAvatar>(out av)) {
            // this new entity is an avatar. If we don't know it already, remember same
            lock (m_avatars) {
                if (!m_avatars.ContainsKey(av.Name.Name)) {
                    LogManager.Log.Log(LogLevel.DUPDATEDETAIL, "AvatarTracker. Update Tracking avatar {0}", av.Name.Name );
                    m_avatars.Add(av.Name.Name, av);
                }
            }
        }
        return;
    }

    void World_OnWorldEntityRemoved(IEntity ent) {
        IEntityAvatar av;
        if (ent.TryGet<IEntityAvatar>(out av)) {
            // this entity is an avatar. If we're tracking it, stop that
            lock (m_avatars) {
                if (m_avatars.ContainsKey(av.Name.Name)) {
                    m_avatars.Remove(av.Name.Name);
                }
            }
        }
        return;
    }


    private OMVSD.OSD GetHandler(RestHandler handler, Uri uri, String after) {
        OMVSD.OSDMap ret = new OMVSD.OSDMap();
        lock (m_avatars) {
            foreach (KeyValuePair<string, IEntityAvatar> kvp in m_avatars) {
                OMVSD.OSDMap oneAV = new OMVSD.OSDMap();
                IEntityAvatar iav = kvp.Value;
                try {
                    oneAV.Add("Name", new OMVSD.OSDString(iav.DisplayName));
                    oneAV.Add("Region", new OMVSD.OSDString(iav.RegionContext.Name.Name));
                    oneAV.Add("X", new OMVSD.OSDString(iav.RegionPosition.X.ToString("###0.###")));
                    oneAV.Add("Y", new OMVSD.OSDString(iav.RegionPosition.Y.ToString("###0.###")));
                    oneAV.Add("Z", new OMVSD.OSDString(iav.RegionPosition.Z.ToString("###0.###")));
                    float dist = 0f;
                    if (m_agentAV != null) {
                        dist = OMV.Vector3.Distance(m_agentAV.RegionPosition, iav.RegionPosition);
                    }
                    oneAV.Add("Distance", new OMVSD.OSDString(dist.ToString("###0.###")));
                    oneAV.Add("Flags", new OMVSD.OSDString(iav.ActivityFlags));
                    if (iav is LLEntityAvatar) {
                        OMV.Avatar av = ((LLEntityAvatar)iav).Avatar;
                        if (av != null) {
                            OMVSD.OSDMap avTextures = new OMVSD.OSDMap();
                            OMV.Primitive.TextureEntry texEnt = av.Textures;
                            if (texEnt != null) {
                                OMV.Primitive.TextureEntryFace[] texFaces = texEnt.FaceTextures;
                                if (texFaces != null) {
                                    if (texFaces[(int)OMV.AvatarTextureIndex.HeadBaked] != null)
                                        avTextures.Add("head", new OMVSD.OSDString(texFaces[(int)OMV.AvatarTextureIndex.HeadBaked].TextureID.ToString()));
                                    if (texFaces[(int)OMV.AvatarTextureIndex.UpperBaked] != null)
                                        avTextures.Add("upper", new OMVSD.OSDString(texFaces[(int)OMV.AvatarTextureIndex.UpperBaked].TextureID.ToString()));
                                    if (texFaces[(int)OMV.AvatarTextureIndex.LowerBaked] != null)
                                        avTextures.Add("lower", new OMVSD.OSDString(texFaces[(int)OMV.AvatarTextureIndex.LowerBaked].TextureID.ToString()));
                                    if (texFaces[(int)OMV.AvatarTextureIndex.EyesBaked] != null)
                                        avTextures.Add("eyes", new OMVSD.OSDString(texFaces[(int)OMV.AvatarTextureIndex.EyesBaked].TextureID.ToString()));
                                    if (texFaces[(int)OMV.AvatarTextureIndex.HairBaked] != null)
                                        avTextures.Add("hair", new OMVSD.OSDString(texFaces[(int)OMV.AvatarTextureIndex.HairBaked].TextureID.ToString()));
                                    if (texFaces[(int)OMV.AvatarTextureIndex.SkirtBaked] != null)
                                        avTextures.Add("skirt", new OMVSD.OSDString(texFaces[(int)OMV.AvatarTextureIndex.SkirtBaked].TextureID.ToString()));
                                    oneAV.Add("LLtextures", avTextures);
                                }
                            }
                        }
                    }
                }
                catch (Exception e) {
                    LogManager.Log.Log(LogLevel.DBADERROR, "AvatarTracker.GetHandler: exception building response: {0}", e);
                }
                ret.Add(kvp.Value.Name.Name.Replace('/', '-'), oneAV);
            }
        }
        return ret;
    }

    private OMVSD.OSD PostHandler(RestHandler handler, Uri uri, String after, OMVSD.OSD body) {
        return new OMVSD.OSDMap();
    }


}
}
