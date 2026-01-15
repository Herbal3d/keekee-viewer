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

using Microsoft.Extensions.Options;

using KeeKee.Config;
using KeeKee.Framework.Logging;
using KeeKee.Renderer;
using KeeKee.World;
using KeeKee.World.LL;

using LibreMetaverse;

using OMV = OpenMetaverse;
using OMVR = OpenMetaverse.Rendering;
using OMVI = OpenMetaverse.Imaging;

namespace KeeKee.Renderer.OGL {
    /// <summary>
    /// A renderer that renders straight to OpenGL/OpenTK
    /// </summary>
    public sealed class RendererOGL : IRenderProvider {
        private KLogger<RendererOGL> m_log;

        private IOptions<RendererOGLConfig> m_options;

        public CameraOGL Camera;

        // private Mesher.MeshmerizerR m_meshMaker = null;
        private OMVR.IRendering m_meshMaker;

        // Textures
        public Dictionary<OMV.UUID, TextureInfo> Textures = new Dictionary<OMV.UUID, TextureInfo>();

        //Terrain
        public float MaxHeight = 0.1f;
        public List<IRegionContext> m_trackedRegions;
        public IRegionContext m_focusRegion;
        public IUserInterfaceProvider UserInterface { get; private set; }

        public RendererOGL(KLogger<RendererOGL> pLog,
                            IOptions<RendererOGLConfig> pOptions,
                            IUserInterfaceProvider pUserInterface,
                            OMVR.IRendering pMeshMaker
                            ) {
            m_log = pLog;
            m_options = pOptions;
            UserInterface = pUserInterface;
            m_meshMaker = pMeshMaker;

            m_log.Log(KLogLevel.DINIT, "RendererOGL starting");

            // default to the class name. The module code can set it to something else later.
            m_trackedRegions = new List<IRegionContext>();
        }

        #region IRenderProvider

        // entry for main thread for rendering. Return false if you don't need it.
        public bool RendererThread() {
            return false;
        }
        // entry for rendering one frame. An alternate to the above thread method
        public bool RenderOneFrame(bool pump, int len) {
            return true;
        }

        //=================================================================
        // Set the entity to be rendered
        public void Render(IEntity ent) {
            if (ent is LLEntity) {
                lock (ent) {
                    CreateNewPrim(ent as LLEntity);
                }
            }
            return;
        }

        private async Task<RenderablePrim?> CreateNewPrim(LLEntity ent) {
            m_log.Log(KLogLevel.DRENDERDETAIL, "Create new prim {0}", ent.Name.Name);
            // entity render info is kept per region. Get the region prim structure
            RegionRenderInfo rri = GetRegionRenderInfo(ent.RegionContext);

            if (ent.HasComponent<ICmptAvatar>()) {
                // if this entity is an avatar, just put it on the display list
                lock (rri.renderAvatarList) {
                    if (!rri.renderAvatarList.ContainsKey(ent.LGID)) {
                        RenderableAvatar ravv = new RenderableAvatar();
                        ravv.Avatar = ent;
                        rri.renderAvatarList.Add(ent.LGID, ravv);
                    }
                }
                return null;
            }
            if (ent.Prim == null) {
                m_log.Log(KLogLevel.DBADERROR, "CreateNewPrim: no prim data for {0}", ent.Name.Name);
                return null;
            }
            OMV.Primitive prim = ent.Prim;

            /* don't do foliage yet
            if (prim.PrimData.PCode == OMV.PCode.Grass 
                        || prim.PrimData.PCode == OMV.PCode.Tree 
                        || prim.PrimData.PCode == OMV.PCode.NewTree) {
                lock (renderFoliageList)
                    renderFoliageList[prim.LocalID] = prim;
                return;
            }
             */

            RenderablePrim render = new RenderablePrim();
            render.Prim = prim;
            render.AContext = ent.AssetContext;
            render.RContext = ent.RegionContext;
            render.Position = prim.Position;
            render.Rotation = prim.Rotation;
            render.IsVisible = true;    // initially assume visible


            if (prim.Sculpt != null) {
                EntityNameLL textureEnt = EntityNameLL.ConvertTextureWorldIDToEntityName(ent.AssetContext, prim.Sculpt.SculptTexture);
                var textureInfo = await ent.AssetContext.DoTextureLoad(textureEnt, AssetType.SculptieTexture);
                textureInfo.AssetData.Decode();
                var textureBitmap = textureInfo.AssetData.Image.ExportBitmap();
                render.Mesh = m_meshMaker.GenerateFacetedSculptMesh(prim, textureBitmap, OMVR.DetailLevel.Medium);
                textureBitmap.Dispose();
            } else {
                render.Mesh = m_meshMaker.GenerateFacetedMesh(prim, OMVR.DetailLevel.High);
            }

            if (render.Mesh == null) {
                // mesh generation failed 
                m_log.Log(KLogLevel.DBADERROR, "FAILED MESH GENERATION: not generating new prim {0}", ent.Name.Name);
                return null;
            }

            // Create a FaceData struct for each face that stores the 3D data
            // in an OpenGL friendly format
            for (int j = 0; j < render.Mesh.Faces.Count; j++) {
                OMVR.Face face = render.Mesh.Faces[j];
                FaceData data = new FaceData();

                // Vertices for this face
                data.Vertices = new float[face.Vertices.Count * 3];
                for (int k = 0; k < face.Vertices.Count; k++) {
                    data.Vertices[k * 3 + 0] = face.Vertices[k].Position.X;
                    data.Vertices[k * 3 + 1] = face.Vertices[k].Position.Y;
                    data.Vertices[k * 3 + 2] = face.Vertices[k].Position.Z;
                }

                // Indices for this face
                data.Indices = face.Indices.ToArray();

                // Texture transform for this face
                OMV.Primitive.TextureEntryFace teFace = prim.Textures.GetFace((uint)j);
                m_meshMaker.TransformTexCoords(face.Vertices, face.Center, teFace, OMV.Vector3.One);

                // Texcoords for this face
                data.TexCoords = new float[face.Vertices.Count * 2];
                for (int k = 0; k < face.Vertices.Count; k++) {
                    data.TexCoords[k * 2 + 0] = face.Vertices[k].TexCoord.X;
                    data.TexCoords[k * 2 + 1] = face.Vertices[k].TexCoord.Y;
                }

                data.Normals = new float[face.Vertices.Count * 3];
                for (int k = 0; k < face.Vertices.Count; k++) {
                    data.Normals[k * 3 + 0] = face.Vertices[k].Normal.X;
                    data.Normals[k * 3 + 1] = face.Vertices[k].Normal.Y;
                    data.Normals[k * 3 + 2] = face.Vertices[k].Normal.Z;
                }


                // m_log.Log(KLogLevel.DRENDERDETAIL, "CreateNewPrim: v={0}, i={1}, t={2}",
                //     data.Vertices.GetLength(0), data.Indices.GetLength(0), data.TexCoords.GetLength(0));

                // Texture for this face
                if (teFace.TextureID != OMV.UUID.Zero &&
                            teFace.TextureID != OMV.Primitive.TextureEntry.WHITE_TEXTURE) {
                    lock (Textures) {
                        if (!Textures.ContainsKey(teFace.TextureID)) {
                            // temporarily add the entry to the table so we don't request it multiple times
                            Textures.Add(teFace.TextureID, new TextureInfo(0, true));
                            // We haven't constructed this image in OpenGL yet, get ahold of it
                            IAssetContext.RequestTextureLoad(
                                EntityNameLL.ConvertTextureWorldIDToEntityName(ent.AssetContext, teFace.TextureID),
                                AssetType.Texture,
                                OnTextureDownloadFinished);
                        }
                    }
                }

                // Set the UserData for this face to our FaceData struct
                face.UserData = data;
                render.Mesh.Faces[j] = face;
            }

            lock (rri.renderPrimList) {
                rri.renderPrimList[prim.LocalID] = render;
            }
        }

        private void OnTextureDownloadFinished(string textureEntityName, bool hasTransparancy) {
            m_log.Log(KLogLevel.DRENDERDETAIL, "OnTextureDownloadFinished {0}", textureEntityName);
            EntityName entName = new EntityName(textureEntityName);
            OMV.UUID id = new OMV.UUID(entName.ExtractEntityFromEntityName());

            TextureInfo info;
            lock (Textures) {
                if (!Textures.TryGetValue(id, out info)) {
                    // The id of zero will say that the mipmaps need to be generated before the texture is used
                    m_log.Log(KLogLevel.DRENDERDETAIL, "Adding TextureInfo for {0}:{1}", entName.Name, id.ToString());
                    info.Alpha = hasTransparancy;
                }
            }
        }

        public void RenderUpdate(LLEntity ent, UpdateCodes what) {
            m_log.Log(KLogLevel.DRENDERDETAIL, "RenderUpdate: {0} for {1}", ent.Name.Name, what);
            bool fullUpdate = false;
            lock (ent) {
                if (ent is LLEntity && ((what & UpdateCodes.New) != 0)) {
                    _ = await CreateNewPrim(ent);
                    fullUpdate = true;
                }
                if ((what & UpdateCodes.Animation) != 0) {
                    // the prim has changed its rotation animation
                    IAnimation anim;
                    if (ent.TryGet<IAnimation>(out anim)) {
                        m_log.Log(KLogLevel.DRENDERDETAIL, "RenderUpdate: animation ");
                        RegionRenderInfo rri;
                        if (ent.RegionContext.TryGet<RegionRenderInfo>(out rri)) {
                            lock (rri) {
                                rri.animations.Add(AnimatBase.CreateAnimation(anim, ((LLEntity)ent).Prim.LocalID));
                            }
                        }
                    }
                }
                if ((what & UpdateCodes.Text) != 0) {
                    // text associated with the prim changed
                    m_log.Log(KLogLevel.DRENDERDETAIL, "RenderUpdate: text changed");
                }
                if ((what & UpdateCodes.Particles) != 0) {
                    // particles associated with the prim changed
                    m_log.Log(KLogLevel.DRENDERDETAIL, "RenderUpdate: particles changed");
                }
                if (!fullUpdate && (what & (UpdateCodes.Scale | UpdateCodes.Position | UpdateCodes.Rotation)) != 0) {
                    // world position has changed. Tell Ogre they have changed
                    try {
                        m_log.Log(KLogLevel.DRENDERDETAIL, "RenderUpdate: Updating position/rotation for {0}", ent.Name.Name);
                        RegionRenderInfo rri;
                        if (ent.RegionContext.TryGet<RegionRenderInfo>(out rri)) {
                            lock (rri.renderPrimList) {
                                // exception if the casting does not work
                                if (((LLEntity)ent).Prim != null) {
                                    uint localID = ((LLEntity)ent).Prim.LocalID;
                                    if (rri.renderPrimList.ContainsKey(localID)) {
                                        RenderablePrim rp = rri.renderPrimList[localID];
                                        rp.Position = new OMV.Vector3(ent.RegionPosition.X, ent.RegionPosition.Y, ent.RegionPosition.Z);
                                        rp.Rotation = new OMV.Quaternion(ent.Heading.X, ent.Heading.Y, ent.Heading.Z, ent.Heading.W);
                                    }
                                }
                            }
                        }
                    } catch (Exception e) {
                        m_log.Log(KLogLevel.DBADERROR, "RenderUpdate: FAIL updating pos/rot: {0}", e);
                    }
                }
            }
            return;
        }

        public void UnRender(IEntity ent) {
            return;
        }

        // tell the renderer about the camera position
        public void UpdateCamera(CameraControl cam) {
            if (m_focusRegion != null) {
                OMV.Vector3 newPos = new OMV.Vector3();
                newPos.X = (float)(cam.GlobalPosition.X - m_focusRegion.GlobalPosition.X);
                newPos.Y = (float)(cam.GlobalPosition.Y - m_focusRegion.GlobalPosition.Y);
                // another kludge camera offset. Pairs with position kludge in Viewer.
                newPos.Z = (float)(cam.GlobalPosition.Z - m_focusRegion.GlobalPosition.Z) + 10f;
                m_log.Log(KLogLevel.DRENDERDETAIL, "UpdateCamera: g={0}, f={1}, n={2}",
                    cam.GlobalPosition.ToString(), m_focusRegion.GlobalPosition.ToString(), newPos.ToString());
                Camera.Position = newPos;
                OMV.Vector3 dir = new OMV.Vector3(1f, 0f, 0f);
                Camera.FocalPoint = (dir * cam.Heading) + Camera.Position;
            }
            return;
        }
        public void UpdateEnvironmentalLights(IEntity pSun, IEntity pMoon) {
            return;
        }

        // Given the current mouse position, return a point in the world
        public OMV.Vector3d SelectPoint() {
            return new OMV.Vector3d(0d, 0d, 0d);
        }

        // rendering specific information for placing in  the view
        public void MapRegionIntoView(IRegionContext rcontext) {
            if (!m_trackedRegions.Contains(rcontext)) {
                m_trackedRegions.Add(rcontext);
            }
            // get the render info block to create it if it doesn't exist
            RegionRenderInfo rri = GetRegionRenderInfo(rcontext);
            return;
        }

        // create and initialize the renderinfoblock
        private RegionRenderInfo GetRegionRenderInfo(IRegionContext rcontext) {
            RegionRenderInfo ret = null;
            if (!rcontext.TryGet<RegionRenderInfo>(out ret)) {
                ret = new RegionRenderInfo();
                rcontext.RegisterInterface<RegionRenderInfo>(ret);
                ret.oceanHeight = rcontext?.TerrainInfo?.WaterHeight ?? 40.0f;
            }
            return ret;
        }

        // Set one region as the focus of display
        public void SetFocusRegion(IRegionContext rcontext) {
            m_focusRegion = rcontext;
            return;
        }

        // something about the terrain has changed, do some updating
        public void UpdateTerrain(IRegionContext rcontext) {
            RegionRenderInfo rri = GetRegionRenderInfo(rcontext);
            // making this true will case the low level renderer to rebuild the terrain
            rri.refreshTerrain = true;
            return;
        }
        #endregion IRenderProvider
    }
}
