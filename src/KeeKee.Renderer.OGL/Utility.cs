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
using KeeKee.World;
using OMV = OpenMetaverse;
using OMVR = OpenMetaverse.Rendering;

namespace KeeKee.Renderer.OGL {

    public struct FaceData {
        public float[] Vertices;
        public ushort[] Indices;
        public float[] Normals;
        public float[] TexCoords;
        public int TexturePointer;
        public System.Drawing.Image Texture;
        // TODO: Normals
    }

    public sealed class TextureInfo {
        /// <summary>OpenGL Texture ID</summary>
        public int ID;
        /// <summary>True if this texture has an alpha component</summary>
        public bool Alpha;

        public TextureInfo(int id, bool alpha) {
            ID = id;
            Alpha = alpha;
        }
    }

    public struct HeightmapLookupValue : IComparable<HeightmapLookupValue> {
        public ushort Index;
        public float Value;

        public HeightmapLookupValue(ushort index, float value) {
            Index = index;
            Value = value;
        }

        public int CompareTo(HeightmapLookupValue val) {
            return Value.CompareTo(val.Value);
        }
    }

    /// <summary>
    /// Rendering information for OpenGL in the region. This is attached the
    /// rcontext as an interface. This holds all the per region information needed
    /// to render the region.
    /// </summary>
    public sealed class RegionRenderInfo {
        public RegionRenderInfo() {
            this.renderFoliageList = new Dictionary<uint, OMV.Primitive>();
            this.renderPrimList = new Dictionary<uint, RenderablePrim>();
            this.renderAvatarList = new Dictionary<ulong, RenderableAvatar>();
            this.animations = new List<AnimatBase>();
            this.oceanHeight = 0f;
            this.terrainWidth = this.terrainLength = -1;
            this.refreshTerrain = true;     // force initial build
        }
        public Dictionary<uint, OMV.Primitive> renderFoliageList;
        public Dictionary<uint, RenderablePrim> renderPrimList;
        public Dictionary<ulong, RenderableAvatar> renderAvatarList;

        public List<AnimatBase> animations;

        public bool refreshTerrain;
        public float[] terrainVertices;
        public float[] terrainTexCoord;
        public float[] terrainNormal;
        public UInt16[] terrainIndices;
        public float terrainWidth;
        public float terrainLength;
        public float oceanHeight;
    }

    /// <summary>
    /// Description of all the information for OpenGL to render the prim.
    /// Kept in a list in the RegionRenderInfo for the region.
    /// </summary>
    public sealed class RenderablePrim {
        public OMV.Primitive Prim { get; set; }     // the prim underlying this
        public OMVR.FacetedMesh Mesh { get; set; }  // meshed prim
        public IRegionContext RContext { get; set; } // used for positioning in displayed world
        public IAssetContext AContext { get; set; }  // used for finding textures for Prim
        public bool IsVisible { get; set; }         // prim is visible from the current camera location

        private OMV.Vector3 localPosition;
        public OMV.Vector3 Position {
            get { return localPosition; }
            set { localPosition = value; }
        }
        private OMV.Quaternion localRotation;
        public OMV.Quaternion Rotation {
            get { return localRotation; }
            set { localRotation = value; }
        }

        public readonly static RenderablePrim Empty = new RenderablePrim();
    }

    public sealed class RenderableAvatar {
        public IEntity Avatar { get; set; }
    }

    public static class Math3D {
        // Column-major:
        // |  0  4  8 12 |
        // |  1  5  9 13 |
        // |  2  6 10 14 |
        // |  3  7 11 15 |

        public static float[] CreateTranslationMatrix(OMV.Vector3 v) {
            float[] mat = new float[16];

            mat[12] = v.X;
            mat[13] = v.Y;
            mat[14] = v.Z;
            mat[0] = mat[5] = mat[10] = mat[15] = 1;

            return mat;
        }

        public static float[] CreateRotationMatrix(OMV.Quaternion q) {
            float[] mat = new float[16];

            // Transpose the quaternion (don't ask me why)
            q.X = q.X * -1f;
            q.Y = q.Y * -1f;
            q.Z = q.Z * -1f;

            float x2 = q.X + q.X;
            float y2 = q.Y + q.Y;
            float z2 = q.Z + q.Z;
            float xx = q.X * x2;
            float xy = q.X * y2;
            float xz = q.X * z2;
            float yy = q.Y * y2;
            float yz = q.Y * z2;
            float zz = q.Z * z2;
            float wx = q.W * x2;
            float wy = q.W * y2;
            float wz = q.W * z2;

            mat[0] = 1.0f - (yy + zz);
            mat[1] = xy - wz;
            mat[2] = xz + wy;
            mat[3] = 0.0f;

            mat[4] = xy + wz;
            mat[5] = 1.0f - (xx + zz);
            mat[6] = yz - wx;
            mat[7] = 0.0f;

            mat[8] = xz - wy;
            mat[9] = yz + wx;
            mat[10] = 1.0f - (xx + yy);
            mat[11] = 0.0f;

            mat[12] = 0.0f;
            mat[13] = 0.0f;
            mat[14] = 0.0f;
            mat[15] = 1.0f;

            return mat;
        }

        public static float[] CreateScaleMatrix(OMV.Vector3 v) {
            float[] mat = new float[16];

            mat[0] = v.X;
            mat[5] = v.Y;
            mat[10] = v.Z;
            mat[15] = 1;

            return mat;
        }
    }
}
