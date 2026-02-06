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

using System.Net;
using System.Text;

using KeeKee.Contexts;
using KeeKee.Entity;
using KeeKee.Framework.Logging;
using KeeKee.Framework.Utilities;
using KeeKee.Rest;

using OMV = OpenMetaverse;
using OMVSD = OpenMetaverse.StructuredData;

namespace KeeKee.World.Services {

    public class RestHandlerAvatarTracker : IRestHandler {

        private readonly KLogger<RestHandlerAvatarTracker> m_log;
        private readonly RestManager m_RestManager;

        // The prefix of the requested URL that is processed by this handler.
        public string Prefix { get; set; }

        protected Dictionary<string, IEntity> m_avatars;

        private IWorld m_world;

        public RestHandlerAvatarTracker(KLogger<RestHandlerAvatarTracker> pLogger,
                                RestManager pRestManager,
                                IWorld pWorld
                                ) {
            m_log = pLogger;
            m_RestManager = pRestManager;
            m_world = pWorld;

            Prefix = Utilities.JoinFilePieces(m_RestManager.APIBase, "/avatars");

            m_RestManager.RegisterListener(this);

            m_avatars = new Dictionary<string, IEntity>();

            m_world.OnAgentNew += new WorldAgentNewCallback(World_OnAgentNew);
            m_world.OnAgentRemoved += new WorldAgentRemovedCallback(World_OnAgentRemoved);
            m_world.OnWorldEntityNew += new WorldEntityNewCallback(World_OnWorldEntityNew);
            m_world.OnWorldEntityUpdate += new WorldEntityUpdateCallback(World_OnWorldEntityUpdate);
            m_world.OnWorldEntityRemoved += new WorldEntityRemovedCallback(World_OnWorldEntityRemoved);
        }

        public async Task ProcessGetOrPostRequest(HttpListenerContext pContext,
                                           HttpListenerRequest pRequest,
                                           HttpListenerResponse pResponse,
                                           CancellationToken pCancelToken) {

            if (pRequest?.HttpMethod.ToUpper().Equals("GET") ?? false) {
                m_log.Log(KLogLevel.RestDetail, "GET: " + (pRequest?.Url?.ToString() ?? "UNKNOWN"));


                OMVSD.OSDMap ret = new OMVSD.OSDMap();
                lock (m_avatars) {
                    foreach (KeyValuePair<string, IEntity> kvp in m_avatars) {
                        OMVSD.OSDMap oneAV = new OMVSD.OSDMap();
                        IEntity iav = kvp.Value;
                        try {
                            oneAV.Add("Name", new OMVSD.OSDString(iav.Cmpt<ICmptAvatar>().DisplayName));
                            oneAV.Add("Region", new OMVSD.OSDString(iav.RegionContext.Name.Name));
                            var loc = iav.Cmpt<ICmptLocation>().RegionPosition;
                            oneAV.Add("X", new OMVSD.OSDString(loc.X.ToString("###0.###")));
                            oneAV.Add("Y", new OMVSD.OSDString(loc.Y.ToString("###0.###")));
                            oneAV.Add("Z", new OMVSD.OSDString(loc.Z.ToString("###0.###")));

                            // Compute distance from main agent if we have one
                            float dist = 0f;
                            if (m_world.Agent != null
                                        && m_world.Agent.HasComponent<ICmptLocation>()
                                        && m_world.Agent.LGID != iav.LGID) {
                                dist = OMV.Vector3.Distance(m_world.Agent.Cmpt<ICmptLocation>().RegionPosition, iav.Cmpt<ICmptLocation>().RegionPosition);
                            }
                            oneAV.Add("Distance", new OMVSD.OSDString(dist.ToString("###0.###")));
                            oneAV.Add("Flags", new OMVSD.OSDString(iav.Cmpt<ICmptAvatar>().ActivityFlags));

                            /* Include detailed texture info if available. (Why are we doing this?)
                            if (iav is LLEntity) {
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
                            */
                        } catch (Exception e) {
                            m_log.Log(KLogLevel.DBADERROR, "AvatarTracker.GetHandler: exception building response: {0}", e);
                        }
                        ret.Add(kvp.Value.Name.Name.Replace('/', '-'), oneAV);
                    }
                }
                string asJson = OMVSD.OSDParser.SerializeJsonString(ret);
                m_RestManager.DoSimpleResponse(pResponse, "application/json", () => Encoding.UTF8.GetBytes(asJson));
                return;
            }
            if (pRequest?.HttpMethod.ToUpper().Equals("POST") ?? false) {
                m_log.Log(KLogLevel.RestDetail, "POST: " + (pRequest?.Url?.ToString() ?? "UNKNOWN"));
            }
        }

        void World_OnAgentNew(IEntity pEnt) {
            // someday remember the main agent for highlighting
            return;
        }

        void World_OnAgentRemoved(IEntity pEnt) {
            return;
        }

        void World_OnWorldEntityNew(IEntity pEnt) {
            if (pEnt.HasComponent<ICmptAvatar>()) {
                // this new entity is an avatar. If we don't know it already, remember same
                lock (m_avatars) {
                    if (!m_avatars.ContainsKey(pEnt.Name.Name)) {
                        m_log.Log(KLogLevel.DUPDATEDETAIL, "AvatarTracker. Tracking avatar {0}", pEnt.Name.Name);
                        m_avatars.Add(pEnt.Name.Name, pEnt);
                    }
                }
            }
            return;
        }

        void World_OnWorldEntityUpdate(IEntity pEnt, UpdateCodes what) {
            if (pEnt.HasComponent<ICmptAvatar>()) {
                // this updated entity is an avatar. If we don't know it already, remember same
                lock (m_avatars) {
                    if (!m_avatars.ContainsKey(pEnt.Name.Name)) {
                        m_log.Log(KLogLevel.DUPDATEDETAIL, "AvatarTracker. Update Tracking avatar {0}", pEnt.Name.Name);
                        m_avatars.Add(pEnt.Name.Name, pEnt);
                    }
                }
            }
            return;
        }

        void World_OnWorldEntityRemoved(IEntity pEnt) {
            if (pEnt.HasComponent<ICmptAvatar>()) {
                // this entity is an avatar. If we're tracking it, stop that
                lock (m_avatars) {
                    if (m_avatars.ContainsKey(pEnt.Name.Name)) {
                        m_avatars.Remove(pEnt.Name.Name);
                    }
                }
            }
            return;
        }

        public void Dispose() {
            m_world.OnAgentNew -= new WorldAgentNewCallback(World_OnAgentNew);
            m_world.OnAgentRemoved -= new WorldAgentRemovedCallback(World_OnAgentRemoved);
            m_world.OnWorldEntityNew -= new WorldEntityNewCallback(World_OnWorldEntityNew);
            m_world.OnWorldEntityUpdate -= new WorldEntityUpdateCallback(World_OnWorldEntityUpdate);
            m_world.OnWorldEntityRemoved -= new WorldEntityRemovedCallback(World_OnWorldEntityRemoved);
        }
    }

}


