// Copyright 2025 Robert Adams
// Copyright (c) 2008 Robert Adams
//
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// Portions of this code were added from LibreMetaverse's MeshmerizerR
// Licensed under 3 clause BSD licence and
// Copyright (c) 2021-2025, Sjofn LLC. All rights reserved.
//
// Portions of this code are:
// Copyright (c) Contributors, http://idealistviewer.org
// The basic logic of the extrusion code is based on the Idealist viewer code.
// The Idealist viewer is licensed under the three clause BSD license.
///
/*
 * MeshmerizerR class implments OpenMetaverse.Rendering.IRendering interface
 * using PrimMesher (http://forge.opensimulator.org/projects/primmesher).
 * The faceted mesh returned is made up of separate face meshes.
 * There are a few additions/changes:
 *  GenerateSimpleMesh() does not generate anything. Use the other mesher for that.
 *  ShouldScaleMesh property sets whether the mesh should be sized up or down
 *      based on the prim scale parameters. If turned off, the mesh will not be
 *      scaled thus allowing the scaling to happen in the graphics library
 *  GenerateScupltMesh() does what it says: takes a bitmap and returns a mesh
 *      based on the RGB coordinates in the bitmap.
 *  TransformTexCoords() does regular transformations but does not do planier
 *      mapping of textures.
 */

using SkiaSharp;

using KeeKee.Framework.Logging;

using LPM = LibreMetaverse.PrimMesher;

using OMV = OpenMetaverse;
using OMVSD = OpenMetaverse.StructuredData;
using OMVR = OpenMetaverse.Rendering;

namespace KeeKee.Renderer {
    /// <summary>
    /// Meshing code based on the Idealist Viewer (20081213).
    /// </summary>

    public class MeshmerizerR : OMVR.IRendering {
        // If this is set to 'true' the returned mesh will be scaled by the prim's scaling
        // parameters. Otherwise the mesh is a unit mesh and needs scaling elsewhere.
        private bool m_shouldScale = true;
        public bool ShouldScaleMesh { get { return m_shouldScale; } set { m_shouldScale = value; } }

        public KLogger<MeshmerizerR> m_log;

        public MeshmerizerR(KLogger<MeshmerizerR> log) {
            m_log = log;
        }

        /// <summary>
        /// Generates a basic mesh structure from a primitive
        /// </summary>
        /// <param name="prim">Primitive to generate the mesh from</param>
        /// <param name="lod">Level of detail to generate the mesh at</param>
        /// <returns>The generated mesh</returns>
        public OMVR.SimpleMesh? GenerateSimpleMesh(OMV.Primitive prim, OMVR.DetailLevel lod) {
            LPM.PrimMesh newPrim = GeneratePrimMesh(prim, lod, false);
            if (newPrim == null)
                return null;

            OMVR.SimpleMesh mesh = new OMVR.SimpleMesh() {
                Path = new OMVR.Path(),
                Prim = prim,
                Profile = new OMVR.Profile(),
                Vertices = new List<OMVR.Vertex>(newPrim.coords.Count)
            };
            foreach (LPM.Coord c in newPrim.coords) {
                mesh.Vertices.Add(new OMVR.Vertex { Position = new OMV.Vector3(c.X, c.Y, c.Z) });
            }

            mesh.Indices = new List<ushort>(newPrim.faces.Count * 3);
            foreach (LPM.Face face in newPrim.faces) {
                mesh.Indices.Add((ushort)face.v1);
                mesh.Indices.Add((ushort)face.v2);
                mesh.Indices.Add((ushort)face.v3);
            }

            return mesh;
        }

        /// <summary>
        /// Generates a basic mesh structure from a primitive, adding normals data.
        /// A 'SimpleMesh' is just the prim's overall shape with no material information.
        /// </summary>
        /// <param name="prim">Primitive to generate the mesh from</param>
        /// <param name="lod">Level of detail to generate the mesh at</param>
        /// <returns>The generated mesh or null on failure</returns>
        public OMVR.SimpleMesh GenerateSimpleMeshWithNormals(OMV.Primitive prim, OMVR.DetailLevel lod) {
            LPM.PrimMesh newPrim = GeneratePrimMesh(prim, lod, true);
            if (newPrim == null)
                return null;

            OMVR.SimpleMesh mesh = new OMVR.SimpleMesh {
                Path = new OMVR.Path(),
                Prim = prim,
                Profile = new OMVR.Profile(),
                Vertices = new List<OMVR.Vertex>(newPrim.coords.Count)
            };

            for (int i = 0; i < newPrim.coords.Count; i++) {
                LPM.Coord c = newPrim.coords[i];
                // Also saving the normal within the vertice
                LPM.Coord n = newPrim.normals[i];
                mesh.Vertices.Add(new OMVR.Vertex { Position = new OMV.Vector3(c.X, c.Y, c.Z), Normal = new OMV.Vector3(n.X, n.Y, n.Z) });
            }

            mesh.Indices = new List<ushort>(newPrim.faces.Count * 3);
            foreach (var face in newPrim.faces) {
                mesh.Indices.Add((ushort)face.v1);
                mesh.Indices.Add((ushort)face.v2);
                mesh.Indices.Add((ushort)face.v3);
            }

            return mesh;
        }

        /// <summary>
        /// Generates a sculpt mesh structure from a primitive
        /// </summary>
        /// <param name="prim">Primitive to generate the mesh from</param>
        /// <param name="lod">Level of detail to generate the mesh at</param>
        /// <returns>The generated mesh</returns>
        public OMVR.SimpleMesh GenerateSimpleSculptMesh(OMV.Primitive prim, SKBitmap bits, OMVR.DetailLevel lod) {
            OMVR.FacetedMesh faceted = GenerateFacetedSculptMesh(prim, bits, lod);

            if (faceted != null && faceted.Faces.Count == 1) {
                OMVR.Face face = faceted.Faces[0];

                OMVR.SimpleMesh mesh = new OMVR.SimpleMesh() {
                    Indices = face.Indices,
                    Vertices = face.Vertices,
                    Path = faceted.Path,
                    Prim = prim,
                    Profile = faceted.Profile
                };

                return mesh;
            }
            return null;
        }

        /// <summary>
        /// Generates a faced sculpt mesh structure from a primitive
        /// </summary>
        /// <param name="prim">Primitive to generate the mesh from</param>
        /// <param name="lod">Level of detail to generate the mesh at</param>
        /// <returns>The generated mesh or 'null' if generation failed</returns>
        public OMVR.FacetedMesh GenerateFacetedSculptMesh(OMV.Primitive prim, SKBitmap bits, OMVR.DetailLevel lod) {
            LPM.SculptMesh.SculptType smSculptType;
            switch (prim.Sculpt.Type) {
                case OpenMetaverse.SculptType.Cylinder:
                    smSculptType = LPM.SculptMesh.SculptType.cylinder;
                    break;
                case OpenMetaverse.SculptType.Plane:
                    smSculptType = LPM.SculptMesh.SculptType.plane;
                    break;
                case OpenMetaverse.SculptType.Sphere:
                    smSculptType = LPM.SculptMesh.SculptType.sphere;
                    break;
                case OpenMetaverse.SculptType.Torus:
                    smSculptType = LPM.SculptMesh.SculptType.torus;
                    break;
                default:
                    smSculptType = LPM.SculptMesh.SculptType.plane;
                    break;
            }
            // The lod for sculpties is the resolution of the texture passed.
            // The first guess is 1:1 then lower resolutions after that
            // int mesherLod = (int)Math.Sqrt(scupltTexture.Width * scupltTexture.Height);
            int mesherLod = 32; // number used in Idealist viewer
            switch (lod) {
                case OMVR.DetailLevel.Highest:
                    break;
                case OMVR.DetailLevel.High:
                    break;
                case OMVR.DetailLevel.Medium:
                    mesherLod /= 2;
                    break;
                case OMVR.DetailLevel.Low:
                    mesherLod /= 4;
                    break;
            }
            LPM.SculptMesh newMesh =
                new LPM.SculptMesh(bits, smSculptType, mesherLod, true, prim.Sculpt.Mirror, prim.Sculpt.Invert);

            int numPrimFaces = 1;       // a scuplty has only one face

            // copy the vertex information into OMVR.IRendering structures
            OMVR.FacetedMesh omvrmesh = new OMVR.FacetedMesh() {
                Faces = new List<OMVR.Face>(),
                Prim = prim,
                Profile = new OMVR.Profile() {
                    Faces = new List<OMVR.ProfileFace>(),
                    Positions = new List<OMV.Vector3>()
                },
                Path = new OMVR.Path() {
                    Points = new List<OMVR.PathPoint>()
                }
            };

            Dictionary<OMVR.Vertex, int> vertexAccount = new Dictionary<OMVR.Vertex, int>();


            for (int ii = 0; ii < numPrimFaces; ii++) {
                vertexAccount.Clear();
                OMVR.Face oface = new OMVR.Face() {
                    Vertices = new List<OMVR.Vertex>(),
                    Indices = new List<ushort>(),
                    TextureFace = prim.Textures == null ? null : prim.Textures.GetFace((uint)ii)
                };
                int faceVertices = newMesh.coords.Count;
                OMVR.Vertex vert;

                for (int j = 0; j < faceVertices; j++) {
                    vert = new OMVR.Vertex() {
                        Position = new OMV.Vector3(newMesh.coords[j].X, newMesh.coords[j].Y, newMesh.coords[j].Z),
                        Normal = new OMV.Vector3(newMesh.normals[j].X, newMesh.normals[j].Y, newMesh.normals[j].Z),
                        TexCoord = new OMV.Vector2(newMesh.uvs[j].U, newMesh.uvs[j].V)
                    };
                    oface.Vertices.Add(vert);
                }

                for (int j = 0; j < newMesh.faces.Count; j++) {
                    oface.Indices.Add((ushort)newMesh.faces[j].v1);
                    oface.Indices.Add((ushort)newMesh.faces[j].v2);
                    oface.Indices.Add((ushort)newMesh.faces[j].v3);
                }

                if (faceVertices > 0) {
                    oface.TextureFace = prim.Textures?.FaceTextures[ii];
                    if (oface.TextureFace == null) {
                        oface.TextureFace = prim.Textures?.DefaultTexture;
                    }
                    oface.ID = ii;
                    omvrmesh.Faces.Add(oface);
                }
            }

            return omvrmesh;
        }

        /// <summary>
        /// Generates a a series of faces, each face containing a mesh and
        /// metadata
        /// </summary>
        /// <param name="prim">Primitive to generate the mesh from</param>
        /// <param name="lod">Level of detail to generate the mesh at</param>
        /// <returns>The generated mesh</returns// >
        public OMVR.FacetedMesh GenerateFacetedMesh(OMV.Primitive prim, OMVR.DetailLevel lod) {
            bool isSphere = ((OMV.ProfileCurve)(prim.PrimData.profileCurve & 0x07) == OMV.ProfileCurve.HalfCircle);
            LPM.PrimMesh newPrim = GeneratePrimMesh(prim, lod, true);
            if (newPrim == null)
                return null;

            int numViewerFaces = newPrim.viewerFaces.Count;
            int numPrimFaces = newPrim.numPrimFaces;

            // TODO: this sphere test is not in LibreMetavere's Meshmerizer. Needed?
            if (isSphere) {
                for (int ii = 0; ii < numPrimFaces; ii++) {
                    // for a sphere, the U texture coordinate goes from 0 to 1
                    // around the sphere. The mesher generates U coordinates
                    // from 0 to 0.5 then -0.5 to 0. So we need to adjust
                    // all U coordinates to be in the 0 to 1 range.
                    LPM.ViewerFace vf = newPrim.viewerFaces[ii];
                    vf.uv1.U = (vf.uv1.U - 0.5f) * 2.0f;
                    vf.uv2.U = (vf.uv2.U - 0.5f) * 2.0f;
                    vf.uv3.U = (vf.uv3.U - 0.5f) * 2.0f;
                    newPrim.viewerFaces[ii] = vf;
                }
            }

            // copy the vertex information into OMVR.IRendering structures
            OMVR.FacetedMesh omvrmesh = new OMVR.FacetedMesh() {
                Faces = new List<OMVR.Face>(),
                Prim = prim,
                Profile = new OMVR.Profile() {
                    Faces = new List<OMVR.ProfileFace>(),
                    Positions = new List<OMV.Vector3>()
                },
                Path = new OMVR.Path() {
                    Points = new List<OMVR.PathPoint>()
                }
            };

            Dictionary<OMV.Vector3, int> vertexAccount = new Dictionary<OMV.Vector3, int>();

            for (int ii = 0; ii < numPrimFaces; ii++) {
                OMVR.Face oface = new OMVR.Face() {
                    Vertices = new List<OMVR.Vertex>(),
                    Indices = new List<ushort>(),
                    TextureFace = prim.Textures.GetFace((uint)ii)
                };

                int faceVertices = 0;
                vertexAccount.Clear();
                OMV.Vector3 pos;
                int indx;
                OMVR.Vertex vert;

                foreach (LPM.ViewerFace vface in newPrim.viewerFaces) {
                    if (vface.primFaceNumber == ii) {
                        faceVertices++;
                        pos = new OMV.Vector3(vface.v1.X, vface.v1.Y, vface.v1.Z);
                        if (vertexAccount.ContainsKey(pos)) {
                            // we aleady have this vertex in the list. Just point the index at it
                            oface.Indices.Add((ushort)vertexAccount[pos]);
                        } else {
                            // the vertex is not in the list. Add it and the new index.
                            vert = new OMVR.Vertex() {
                                Position = pos,
                                TexCoord = new OMV.Vector2(vface.uv1.U, 1.0f - vface.uv1.V),
                                Normal = new OMV.Vector3(vface.n1.X, vface.n1.Y, vface.n1.Z)
                            };
                            oface.Vertices.Add(vert);
                            indx = oface.Vertices.Count - 1;
                            vertexAccount.Add(pos, indx);
                            oface.Indices.Add((ushort)indx);
                        }

                        pos = new OMV.Vector3(vface.v2.X, vface.v2.Y, vface.v2.Z);
                        if (vertexAccount.ContainsKey(pos)) {
                            oface.Indices.Add((ushort)vertexAccount[pos]);
                        } else {
                            vert = new OMVR.Vertex() {
                                Position = pos,
                                TexCoord = new OMV.Vector2(vface.uv2.U, 1.0f - vface.uv2.V),
                                Normal = new OMV.Vector3(vface.n2.X, vface.n2.Y, vface.n2.Z)
                            };
                            oface.Vertices.Add(vert);
                            indx = oface.Vertices.Count - 1;
                            vertexAccount.Add(pos, indx);
                            oface.Indices.Add((ushort)indx);
                        }

                        pos = new OMV.Vector3(vface.v3.X, vface.v3.Y, vface.v3.Z);
                        if (vertexAccount.ContainsKey(pos)) {
                            oface.Indices.Add((ushort)vertexAccount[pos]);
                        } else {
                            vert = new OMVR.Vertex() {
                                Position = pos,
                                TexCoord = new OMV.Vector2(vface.uv3.U, 1.0f - vface.uv3.V),
                                Normal = new OMV.Vector3(vface.n3.X, vface.n3.Y, vface.n3.Z)
                            };
                            oface.Vertices.Add(vert);
                            indx = oface.Vertices.Count - 1;
                            vertexAccount.Add(pos, indx);
                            oface.Indices.Add((ushort)indx);
                        }
                    }
                }
                if (faceVertices > 0) {
                    oface.TextureFace = prim.Textures.FaceTextures[ii];
                    if (oface.TextureFace == null) {
                        oface.TextureFace = prim.Textures.DefaultTexture;
                    }
                    oface.ID = ii;
                    omvrmesh.Faces.Add(oface);
                }
            }

            return omvrmesh;
        }

        private OMVR.FacetedMesh GenerateIRendererMesh(int numPrimFaces, OMV.Primitive prim,
                                                 List<LPM.ViewerFace> viewerFaces) {
            // copy the vertex information into OMVR.IRendering structures
            OMVR.FacetedMesh omvrmesh = new OMVR.FacetedMesh() {
                Faces = new List<OMVR.Face>(),
                Prim = prim,
                Profile = new OMVR.Profile() {
                    Faces = new List<OMVR.ProfileFace>(),
                    Positions = new List<OMV.Vector3>()
                },
                Path = new OMVR.Path() {
                    Points = new List<OMVR.PathPoint>()
                }
            };

            Dictionary<OMV.Vector3, int> vertexAccount = new Dictionary<OMV.Vector3, int>();
            OMV.Vector3 pos;
            int indx;
            OMVR.Vertex vert;
            for (int ii = 0; ii < numPrimFaces; ii++) {
                OMVR.Face oface = new OMVR.Face() {
                    Vertices = new List<OMVR.Vertex>(),
                    Indices = new List<ushort>(),
                    TextureFace = prim.Textures == null ? null : prim.Textures.GetFace((uint)ii)
                };

                int faceVertices = 0;
                vertexAccount.Clear();
                foreach (LPM.ViewerFace vface in viewerFaces) {
                    if (vface.primFaceNumber == ii) {
                        faceVertices++;
                        pos = new OMV.Vector3(vface.v1.X, vface.v1.Y, vface.v1.Z);
                        if (vertexAccount.ContainsKey(pos)) {
                            oface.Indices.Add((ushort)vertexAccount[pos]);
                        } else {
                            vert = new OMVR.Vertex() {
                                Position = pos,
                                TexCoord = new OMV.Vector2(vface.uv1.U, vface.uv1.V),
                                Normal = new OMV.Vector3(vface.n1.X, vface.n1.Y, vface.n1.Z)
                            };
                            vert.Normal.Normalize();

                            oface.Vertices.Add(vert);

                            indx = oface.Vertices.Count - 1;
                            vertexAccount.Add(pos, indx);
                            oface.Indices.Add((ushort)indx);
                        }

                        pos = new OMV.Vector3(vface.v2.X, vface.v2.Y, vface.v2.Z);
                        if (vertexAccount.ContainsKey(pos)) {
                            oface.Indices.Add((ushort)vertexAccount[pos]);
                        } else {
                            vert = new OMVR.Vertex() {
                                Position = pos,
                                TexCoord = new OMV.Vector2(vface.uv2.U, vface.uv2.V),
                                Normal = new OMV.Vector3(vface.n2.X, vface.n2.Y, vface.n2.Z),
                            };
                            vert.Normal.Normalize();

                            oface.Vertices.Add(vert);

                            indx = oface.Vertices.Count - 1;
                            vertexAccount.Add(pos, indx);
                            oface.Indices.Add((ushort)indx);
                        }

                        pos = new OMV.Vector3(vface.v3.X, vface.v3.Y, vface.v3.Z);
                        if (vertexAccount.ContainsKey(pos)) {
                            oface.Indices.Add((ushort)vertexAccount[pos]);
                        } else {
                            vert = new OMVR.Vertex() {
                                Position = pos,
                                TexCoord = new OMV.Vector2(vface.uv3.U, vface.uv3.V),
                                Normal = new OMV.Vector3(vface.n3.X, vface.n3.Y, vface.n3.Z)
                            };
                            vert.Normal.Normalize();

                            oface.Vertices.Add(vert);

                            indx = oface.Vertices.Count - 1;
                            vertexAccount.Add(pos, indx);
                            oface.Indices.Add((ushort)indx);
                        }
                    }
                }
                if (faceVertices > 0) {
                    oface.TextureFace = null;
                    if (prim.Textures != null) {
                        oface.TextureFace = prim.Textures.FaceTextures[ii];
                        if (oface.TextureFace == null) {
                            oface.TextureFace = prim.Textures.DefaultTexture;
                        }
                    }
                    oface.ID = ii;
                    omvrmesh.Faces.Add(oface);
                }
            }

            return omvrmesh;
        }

        /// <summary>
        /// Apply texture coordinate modifications from a
        /// <seealso cref="TextureEntryFace"/> to a list of vertices
        /// </summary>
        /// <param name="vertices">Vertex list to modify texture coordinates for</param>
        /// <param name="center">Center-point of the face</param>
        /// <param name="teFace">Face texture parameters</param>
        public void TransformTexCoords(List<OMVR.Vertex> vertices, OMV.Vector3 center, OMV.Primitive.TextureEntryFace teFace, OMV.Vector3 primScale) {
            // compute trig stuff up front
            float cosineAngle = (float)Math.Cos(teFace.Rotation);
            float sinAngle = (float)Math.Sin(teFace.Rotation);

            // need a check for plainer vs default
            // just do default for now (I don't know what planar is)
            for (int ii = 0; ii < vertices.Count; ii++) {
                // tex coord comes to us as a number between zero and one
                // transform about the center of the texture
                OMVR.Vertex vert = vertices[ii];

                // aply planar tranforms to the UV first if applicable
                if (teFace.TexMapType == OMV.MappingType.Planar) {
                    OMV.Vector3 binormal;
                    float d = OMV.Vector3.Dot(vert.Normal, OMV.Vector3.UnitX);
                    if (d >= 0.5f || d <= -0.5f) {
                        binormal = OMV.Vector3.UnitY;
                        if (vert.Normal.X < 0f) binormal *= -1;
                    } else {
                        binormal = OMV.Vector3.UnitX;
                        if (vert.Normal.Y > 0f) binormal *= -1;
                    }
                    OMV.Vector3 tangent = binormal % vert.Normal;
                    OMV.Vector3 scaledPos = vert.Position * primScale;
                    vert.TexCoord.X = 1f + (OMV.Vector3.Dot(binormal, scaledPos) * 2f - 0.5f);
                    vert.TexCoord.Y = -(OMV.Vector3.Dot(tangent, scaledPos) * 2f - 0.5f);
                }

                float repeatU = teFace.RepeatU;
                float repeatV = teFace.RepeatV;
                float tX = vert.TexCoord.X - 0.5f;
                float tY = vert.TexCoord.Y - 0.5f;

                // rotate, scale, offset
                vert.TexCoord.X = (tX * cosineAngle - tY * sinAngle) * teFace.RepeatU - teFace.OffsetU + 0.5f;
                vert.TexCoord.Y = (tX * sinAngle + tY * cosineAngle) * teFace.RepeatV - teFace.OffsetV + 0.5f;
                vertices[ii] = vert;
            }
            return;
        }

        // The mesh reader code is organized so it can be used in several different ways:
        //
        // 1. Fetch the highest detail displayable mesh as a FacetedMesh:
        //      var facetedMesh = GenerateFacetedMeshMesh(prim, meshData);
        // 2. Get the header, examine the submeshes available, and extract the part
        //              desired (good if getting a different LOD of mesh):
        //      OSDMap meshParts = UnpackMesh(meshData);
        //      if (meshParts.ContainsKey("medium_lod"))
        //          var facetedMesh = MeshSubMeshAsFacetedMesh(prim, meshParts["medium_lod"]):
        // 3. Get a simple mesh from one of the submeshes (good if just getting a physics version):
        //      OSDMap meshParts = UnpackMesh(meshData);
        //      Mesh flatMesh = MeshSubMeshAsSimpleMesh(prim, meshParts["physics_mesh"]);
        //
        // "physics_convex" is specially formatted so there is another routine to unpack
        //              that section:
        //      OSDMap meshParts = UnpackMesh(meshData);
        //      if (meshParts.ContainsKey("physics_convex"))
        //          OSMap hullPieces = MeshSubMeshAsConvexHulls(prim, meshParts["physics_convex"]):
        //
        // LL mesh format detailed at http://wiki.secondlife.com/wiki/Mesh/Mesh_Asset_Format

        /// <summary>
        /// Create a mesh faceted mesh from the compressed mesh data.
        /// This returns the highest LOD renderable version of the mesh.
        ///
        /// The actual mesh data is fetched and passed to this
        /// routine since all the context for finding the data is elsewhere.
        /// </summary>
        /// <returns>The faceted mesh or null if can't do it</returns>
        public OMVR.FacetedMesh GenerateFacetedMeshMesh(OMV.Primitive prim, byte[] meshData) {
            OMVR.FacetedMesh ret = null;
            OMVSD.OSDMap meshParts = UnpackMesh(meshData);
            if (meshParts != null) {
                byte[] meshBytes = null;
                string[] decreasingLOD = { "high_lod", "medium_lod", "low_lod", "lowest_lod" };
                foreach (string partName in decreasingLOD) {
                    if (meshParts.TryGetValue(partName, out var part)) {
                        meshBytes = part;
                        break;
                    }
                }
                if (meshBytes != null) {
                    ret = MeshSubMeshAsFacetedMesh(prim, meshBytes);
                }

            }
            return ret;
        }

        // A version of GenerateFacetedMeshMesh that takes LOD spec so it's similar in calling convention of
        //    the other Generate* methods.
        public OMVR.FacetedMesh? GenerateFacetedMeshMesh(OMV.Primitive prim, byte[] meshData, OMVR.DetailLevel lod) {
            OMVR.FacetedMesh? ret = null;
            string? partName = null;
            switch (lod) {
                case OMVR.DetailLevel.Highest:
                    partName = "high_lod"; break;
                case OMVR.DetailLevel.High:
                    partName = "medium_lod"; break;
                case OMVR.DetailLevel.Medium:
                    partName = "low_lod"; break;
                case OMVR.DetailLevel.Low:
                    partName = "lowest_lod"; break;
            }
            if (partName != null) {
                OMVSD.OSDMap meshParts = UnpackMesh(meshData);
                if (meshParts != null) {
                    if (meshParts.TryGetValue(partName, out var meshBytes)) {
                        if (meshBytes != null) {
                            ret = MeshSubMeshAsFacetedMesh(prim, meshBytes);
                        }
                    }
                }
            }
            return ret;
        }

        // Convert a compressed submesh buffer into a FacetedMesh.
        public OMVR.FacetedMesh? MeshSubMeshAsFacetedMesh(OMV.Primitive prim, byte[] compressedMeshData) {
            OMVR.FacetedMesh? ret = null;
            OMVSD.OSD meshOSD = OMV.Helpers.DecompressOSD(compressedMeshData);

            if (meshOSD is OMVSD.OSDArray meshFaces) {
                ret = new OMVR.FacetedMesh { Faces = new List<OMVR.Face>() };
                for (int faceIndex = 0; faceIndex < meshFaces.Count; faceIndex++) {
                    AddSubMesh(prim, faceIndex, meshFaces[faceIndex], ref ret);
                }
            }
            return ret;
        }


        // Convert a compressed submesh buffer into a SimpleMesh.
        public OMVR.SimpleMesh MeshSubMeshAsSimpleMesh(OMV.Primitive prim, byte[] compressedMeshData) {
            OMVR.SimpleMesh ret = null;
            OMVSD.OSD meshOSD = OMV.Helpers.DecompressOSD(compressedMeshData);

            OMVSD.OSDArray meshFaces = meshOSD as OMVSD.OSDArray;
            if (meshOSD != null) {
                ret = new OMVR.SimpleMesh();
                if (meshFaces != null) {
                    foreach (OMVSD.OSD subMesh in meshFaces) {
                        AddSubMesh(subMesh, ref ret);
                    }
                }
            }
            return ret;
        }

        public List<List<OMV.Vector3>> MeshSubMeshAsConvexHulls(OMV.Primitive prim, byte[] compressedMeshData) {
            List<List<OMV.Vector3>> hulls = new List<List<OMV.Vector3>>();
            try {
                OMVSD.OSD convexBlockOsd = OMV.Helpers.DecompressOSD(compressedMeshData);

                if (convexBlockOsd is OMVSD.OSDMap convexBlock) {
                    OMV.Vector3 min = new OMV.Vector3(-0.5f, -0.5f, -0.5f);
                    if (convexBlock.ContainsKey("Min")) min = convexBlock["Min"].AsVector3();
                    OMV.Vector3 max = new OMV.Vector3(0.5f, 0.5f, 0.5f);
                    if (convexBlock.ContainsKey("Max")) max = convexBlock["Max"].AsVector3();

                    if (convexBlock.ContainsKey("BoundingVerts")) {
                        byte[] boundingVertsBytes = convexBlock["BoundingVerts"].AsBinary();
                        var boundingHull = new List<OMV.Vector3>();
                        for (int i = 0; i < boundingVertsBytes.Length;) {
                            ushort uX = OMV.Utils.BytesToUInt16(boundingVertsBytes, i); i += 2;
                            ushort uY = OMV.Utils.BytesToUInt16(boundingVertsBytes, i); i += 2;
                            ushort uZ = OMV.Utils.BytesToUInt16(boundingVertsBytes, i); i += 2;

                            OMV.Vector3 pos = new OMV.Vector3(
                                OMV.Utils.UInt16ToFloat(uX, min.X, max.X),
                                OMV.Utils.UInt16ToFloat(uY, min.Y, max.Y),
                                OMV.Utils.UInt16ToFloat(uZ, min.Z, max.Z)
                            );

                            boundingHull.Add(pos);
                        }

                        List<OMV.Vector3> mBoundingHull = boundingHull;
                    }

                    if (convexBlock.ContainsKey("HullList")) {
                        byte[] hullList = convexBlock["HullList"].AsBinary();

                        byte[] posBytes = convexBlock["Positions"].AsBinary();

                        int posNdx = 0;

                        foreach (byte cnt in hullList) {
                            int count = cnt == 0 ? 256 : cnt;
                            List<OMV.Vector3> hull = new List<OMV.Vector3>();

                            for (int i = 0; i < count; i++) {
                                ushort uX = OMV.Utils.BytesToUInt16(posBytes, posNdx); posNdx += 2;
                                ushort uY = OMV.Utils.BytesToUInt16(posBytes, posNdx); posNdx += 2;
                                ushort uZ = OMV.Utils.BytesToUInt16(posBytes, posNdx); posNdx += 2;

                                OMV.Vector3 pos = new OMV.Vector3(
                                    OMV.Utils.UInt16ToFloat(uX, min.X, max.X),
                                    OMV.Utils.UInt16ToFloat(uY, min.Y, max.Y),
                                    OMV.Utils.UInt16ToFloat(uZ, min.Z, max.Z)
                                );

                                hull.Add(pos);
                            }

                            hulls.Add(hull);
                        }
                    }
                }
            } catch (Exception) {
                // Logger.Log.WarnFormat("{0} exception decoding convex block: {1}", LogHeader, e);
            }
            return hulls;
        }

        // Add the submesh to the passed SimpleMesh
        private void AddSubMesh(OMVSD.OSD subMeshOsd, ref OMVR.SimpleMesh holdingMesh) {
            if (subMeshOsd is OMVSD.OSDMap subMeshMap) {
                // As per http://wiki.secondlife.com/wiki/Mesh/Mesh_Asset_Format, some Mesh Level
                // of Detail Blocks (maps) contain just a NoGeometry key to signal there is no
                // geometry for this submesh.
                if (subMeshMap.ContainsKey("NoGeometry") && ((OMVSD.OSDBoolean)subMeshMap["NoGeometry"]))
                    return;

                holdingMesh.Vertices.AddRange(CollectVertices(subMeshMap));
                holdingMesh.Indices.AddRange(CollectIndices(subMeshMap));
            }
        }

        // Add the submesh to the passed FacetedMesh as a new face.
        private void AddSubMesh(OMV.Primitive prim, int faceIndex, OMVSD.OSD subMeshOsd, ref OMVR.FacetedMesh holdingMesh) {
            if (subMeshOsd is OMVSD.OSDMap subMesh) {
                // As per http://wiki.secondlife.com/wiki/Mesh/Mesh_Asset_Format, some Mesh Level
                // of Detail Blocks (maps) contain just a NoGeometry key to signal there is no
                // geometry for this submesh.
                if (subMesh.ContainsKey("NoGeometry") && ((OMVSD.OSDBoolean)subMesh["NoGeometry"]))
                    return;

                OMVR.Face oface = new OMVR.Face {
                    ID = faceIndex,
                    Vertices = new List<OMVR.Vertex>(),
                    Indices = new List<ushort>(),
                    TextureFace = prim.Textures.GetFace((uint)faceIndex)
                };

                OMVSD.OSDMap subMeshMap = subMesh;

                oface.Vertices = CollectVertices(subMeshMap);
                oface.Indices = CollectIndices(subMeshMap);

                holdingMesh.Faces.Add(oface);
            }
        }

        private List<OMVR.Vertex> CollectVertices(OMVSD.OSDMap subMeshMap) {
            List<OMVR.Vertex> vertices = new List<OMVR.Vertex>();

            OMV.Vector3 posMax;
            OMV.Vector3 posMin;

            // If PositionDomain is not specified, the default is from -0.5 to 0.5
            if (subMeshMap.ContainsKey("PositionDomain")) {
                posMax = ((OMVSD.OSDMap)subMeshMap["PositionDomain"])["Max"];
                posMin = ((OMVSD.OSDMap)subMeshMap["PositionDomain"])["Min"];
            } else {
                posMax = new OMV.Vector3(0.5f, 0.5f, 0.5f);
                posMin = new OMV.Vector3(-0.5f, -0.5f, -0.5f);
            }

            // Vertex positions
            byte[] posBytes = subMeshMap["Position"];

            // Normals
            byte[] norBytes = null;
            if (subMeshMap.TryGetValue("Normal", out var normal)) {
                norBytes = normal;
            }

            // UV texture map
            OMV.Vector2 texPosMax = OMV.Vector2.Zero;
            OMV.Vector2 texPosMin = OMV.Vector2.Zero;
            byte[] texBytes = null;
            if (subMeshMap.TryGetValue("TexCoord0", out var texCoord0)) {
                texBytes = texCoord0;
                texPosMax = ((OMVSD.OSDMap)subMeshMap["TexCoord0Domain"])["Max"];
                texPosMin = ((OMVSD.OSDMap)subMeshMap["TexCoord0Domain"])["Min"];
            }

            // Extract the vertex position data
            // If present normals and texture coordinates too
            for (int i = 0; i < posBytes.Length; i += 6) {
                ushort uX = OMV.Utils.BytesToUInt16(posBytes, i);
                ushort uY = OMV.Utils.BytesToUInt16(posBytes, i + 2);
                ushort uZ = OMV.Utils.BytesToUInt16(posBytes, i + 4);

                OMVR.Vertex vx = new OMVR.Vertex {
                    Position = new OMV.Vector3(
                        OMV.Utils.UInt16ToFloat(uX, posMin.X, posMax.X),
                        OMV.Utils.UInt16ToFloat(uY, posMin.Y, posMax.Y),
                        OMV.Utils.UInt16ToFloat(uZ, posMin.Z, posMax.Z))
                };


                if (norBytes != null && norBytes.Length >= i + 4) {
                    ushort nX = OMV.Utils.BytesToUInt16(norBytes, i);
                    ushort nY = OMV.Utils.BytesToUInt16(norBytes, i + 2);
                    ushort nZ = OMV.Utils.BytesToUInt16(norBytes, i + 4);

                    vx.Normal = new OMV.Vector3(
                        OMV.Utils.UInt16ToFloat(nX, posMin.X, posMax.X),
                        OMV.Utils.UInt16ToFloat(nY, posMin.Y, posMax.Y),
                        OMV.Utils.UInt16ToFloat(nZ, posMin.Z, posMax.Z));
                }

                var vertexIndexOffset = vertices.Count * 4;

                if (texBytes != null && texBytes.Length >= vertexIndexOffset + 4) {
                    ushort tX = OMV.Utils.BytesToUInt16(texBytes, vertexIndexOffset);
                    ushort tY = OMV.Utils.BytesToUInt16(texBytes, vertexIndexOffset + 2);

                    vx.TexCoord = new OMV.Vector2(
                        OMV.Utils.UInt16ToFloat(tX, texPosMin.X, texPosMax.X),
                        OMV.Utils.UInt16ToFloat(tY, texPosMin.Y, texPosMax.Y));
                }

                vertices.Add(vx);
            }
            return vertices;
        }

        private List<ushort> CollectIndices(OMVSD.OSDMap subMeshMap) {
            List<ushort> indices = new List<ushort>();

            byte[] triangleBytes = subMeshMap["TriangleList"];
            for (int i = 0; i < triangleBytes.Length; i += 6) {
                ushort v1 = (ushort)(OMV.Utils.BytesToUInt16(triangleBytes, i));
                indices.Add(v1);
                ushort v2 = (ushort)(OMV.Utils.BytesToUInt16(triangleBytes, i + 2));
                indices.Add(v2);
                ushort v3 = (ushort)(OMV.Utils.BytesToUInt16(triangleBytes, i + 4));
                indices.Add(v3);
            }
            return indices;
        }

        /// <summary>Decodes mesh asset.</summary>
        /// <returns>OSDMap of all submeshes in the mesh. The value of the submesh name
        /// is the uncompressed data for that mesh.
        /// The OSDMap is made up of the asset_header section (which includes a lot of stuff)
        /// plus each of the submeshes unpacked into compressed byte arrays.</returns>
        public OMVSD.OSDMap? UnpackMesh(byte[] assetData) {
            OMVSD.OSDMap? meshData = new OMVSD.OSDMap();
            try {
                using (MemoryStream data = new MemoryStream(assetData)) {
                    OMVSD.OSDMap header = (OMVSD.OSDMap)OMVSD.OSDParser.DeserializeLLSDBinary(data);
                    meshData["asset_header"] = header;
                    long start = data.Position;

                    foreach (string partName in header.Keys) {
                        if (header[partName].Type != OMVSD.OSDType.Map) {
                            meshData[partName] = header[partName];
                            continue;
                        }

                        OMVSD.OSDMap partInfo = (OMVSD.OSDMap)header[partName];
                        if (partInfo["offset"] < 0 || partInfo["size"] == 0) {
                            meshData[partName] = partInfo;
                            continue;
                        }

                        byte[] part = new byte[partInfo["size"]];
                        Buffer.BlockCopy(assetData, partInfo["offset"] + (int)start, part, 0, part.Length);
                        meshData[partName] = part;
                        // meshData[partName] = Helpers.ZDecompressOSD(part);   // Do decompression at unpack time
                    }
                }
            } catch (Exception ex) {
                m_log.Log(KLogLevel.DBADERROR, "Failed to decode mesh asset", ex);
                meshData = null;
            }
            return meshData;
        }


        // Local routine to create a mesh from prim parameters.
        // Collects parameters and calls PrimMesher to create all the faces of the prim.
        private LPM.PrimMesh GeneratePrimMesh(OMV.Primitive prim, OMVR.DetailLevel lod, bool viewerMode) {
            OMV.Primitive.ConstructionData primData = prim.PrimData;
            int sides = 4;
            int hollowsides = 4;

            float profileBegin = primData.ProfileBegin;
            float profileEnd = primData.ProfileEnd;

            if ((OMV.ProfileCurve)(primData.profileCurve & 0x07) == OMV.ProfileCurve.Circle) {
                switch (lod) {
                    case OMVR.DetailLevel.Low:
                        sides = 6;
                        break;
                    case OMVR.DetailLevel.Medium:
                        sides = 12;
                        break;
                    default:
                        sides = 24;
                        break;
                }
            } else if ((OMV.ProfileCurve)(primData.profileCurve & 0x07) == OMV.ProfileCurve.EqualTriangle)
                sides = 3;
            else if ((OMV.ProfileCurve)(primData.profileCurve & 0x07) == OMV.ProfileCurve.HalfCircle) {
                // half circle, prim is a sphere
                switch (lod) {
                    case OMVR.DetailLevel.Low:
                        sides = 6;
                        break;
                    case OMVR.DetailLevel.Medium:
                        sides = 12;
                        break;
                    default:
                        sides = 24;
                        break;
                }
                profileBegin = 0.5f * profileBegin + 0.5f;
                profileEnd = 0.5f * profileEnd + 0.5f;
            }

            if ((OMV.HoleType)primData.ProfileHole == OMV.HoleType.Same)
                hollowsides = sides;
            else if ((OMV.HoleType)primData.ProfileHole == OMV.HoleType.Circle) {
                switch (lod) {
                    case OMVR.DetailLevel.Low:
                        hollowsides = 6;
                        break;
                    case OMVR.DetailLevel.Medium:
                        hollowsides = 12;
                        break;
                    default:
                        hollowsides = 24;
                        break;
                }
            } else if ((OMV.HoleType)primData.ProfileHole == OMV.HoleType.Triangle)
                hollowsides = 3;

            LPM.PrimMesh newPrim =
                new LPM.PrimMesh(sides, profileBegin, profileEnd, (float)primData.ProfileHollow, hollowsides) {
                    viewerMode = viewerMode,
                    holeSizeX = primData.PathScaleX,
                    holeSizeY = primData.PathScaleY,
                    pathCutBegin = primData.PathBegin,
                    pathCutEnd = primData.PathEnd,
                    topShearX = primData.PathShearX,
                    topShearY = primData.PathShearY,
                    radius = primData.PathRadiusOffset,
                    revolutions = primData.PathRevolutions,
                    skew = primData.PathSkew
                };
            switch (lod) {
                case OMVR.DetailLevel.Low:
                    newPrim.stepsPerRevolution = 6;
                    break;
                case OMVR.DetailLevel.Medium:
                    newPrim.stepsPerRevolution = 12;
                    break;
                default:
                    newPrim.stepsPerRevolution = 24;
                    break;
            }

            if ((primData.PathCurve == OMV.PathCurve.Line) || (primData.PathCurve == OMV.PathCurve.Flexible)) {
                newPrim.taperX = 1.0f - primData.PathScaleX;
                newPrim.taperY = 1.0f - primData.PathScaleY;
                newPrim.twistBegin = (int)(180 * primData.PathTwistBegin);
                newPrim.twistEnd = (int)(180 * primData.PathTwist);
                newPrim.ExtrudeLinear();
            } else {
                newPrim.taperX = primData.PathTaperX;
                newPrim.taperY = primData.PathTaperY;
                newPrim.twistBegin = (int)(360 * primData.PathTwistBegin);
                newPrim.twistEnd = (int)(360 * primData.PathTwist);
                newPrim.ExtrudeCircular();
            }

            return newPrim;
        }

        /// <summary>
        /// Method for generating mesh Face from a heightmap
        /// </summary>
        /// <param name="zMap">Two dimension array of floats containing height information</param>
        /// <param name="xBegin">Starting value for X</param>
        /// <param name="xEnd">Max value for X</param>
        /// <param name="yBegin">Starting value for Y</param>
        /// <param name="yEnd">Max value of Y</param>
        /// <returns></returns>
        public OMVR.Face TerrainMesh(float[,] zMap, float xBegin, float xEnd, float yBegin, float yEnd) {
            LPM.SculptMesh newMesh = new LPM.SculptMesh(zMap, xBegin, xEnd, yBegin, yEnd, true);
            OMVR.Face terrain = new OMVR.Face();
            int faceVertices = newMesh.coords.Count;
            terrain.Vertices = new List<OMVR.Vertex>(faceVertices);
            terrain.Indices = new List<ushort>(newMesh.faces.Count * 3);

            for (int j = 0; j < faceVertices; j++) {
                var vert = new OMVR.Vertex() {
                    Position = new OMV.Vector3(newMesh.coords[j].X, newMesh.coords[j].Y, newMesh.coords[j].Z),
                    Normal = new OMV.Vector3(newMesh.normals[j].X, newMesh.normals[j].Y, newMesh.normals[j].Z),
                    TexCoord = new OMV.Vector2(newMesh.uvs[j].U, newMesh.uvs[j].V)
                };
                terrain.Vertices.Add(vert);
            }

            for (int j = 0; j < newMesh.faces.Count; j++) {
                terrain.Indices.Add((ushort)newMesh.faces[j].v1);
                terrain.Indices.Add((ushort)newMesh.faces[j].v2);
                terrain.Indices.Add((ushort)newMesh.faces[j].v3);
            }

            return terrain;
        }
    }
}
