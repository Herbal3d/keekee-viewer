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

using KeeKee.Contexts;
using KeeKee.Entity;

namespace KeeKee.Renderer {
    /// <summary>
    /// There is a cross product of potential world format types and render
    /// display formats. This class contains routines for the conversion
    /// of some world type (LL prim, for instance) to renderer display
    /// formats (Mogre mesh).
    /// </summary>
    public interface IWorldRenderConv {

        /// <summary>
        /// Collect rendering info. The information collected for rendering has a pre
        /// phase (this call), a doit phase and then a post phase (usually on demand
        /// requests).
        /// If we can't collect all the information return null. For LLLP, the one thing
        /// we might not have is the parent entity since child prims are rendered relative
        /// to the parent.
        /// This will be called multiple times trying to get the information for the 
        /// renderable. The callCount is the number of times we have asked. The caller
        /// can pass zero and know nothing will happen. Values more than zero can cause
        /// this routine to try and do some implementation specific thing to fix the
        /// problem. For LLLP, this is usually asking for the parent to be loaded.
        /// </summary>
        /// <param name="priority"></param>
        /// <param name="sceneMgr"></param>
        /// <param name="ent"></param>
        /// <param name="callCount">zero if do nothing, otherwise the number of times that
        /// this RenderingInfo has been asked for</param>
        /// <returns>rendering info or null if we cannot collect all data</returns>
        RenderableInfo RenderingInfo(float priority, Object sceneMgr, IEntity ent, int callCount);

        /// <summary>
        /// If doing mesh creation post processing, this causes the mesh resource to
        /// be created from the passed, world specific entity information.
        /// </summary>
        /// <param name="priority"></param>
        /// <param name="ent"></param>
        /// <returns>false if we need to wait for resources before completing mesh creation</returns>
        bool CreateMeshResource(float priority, IEntity ent, string meshName, EntityName contextEntityName);

        /// <summary>
        /// Create a mesh for an avatar. This creates the mesh in the Ogre world of the passed name.
        /// </summary>
        /// <param name="priority"></param>
        /// <param name="ent"></param>
        /// <returns>false if we need to wait for resources before completing mesh creation</returns>
        bool CreateAvatarMeshResource(float priority, IEntity ent, string meshName, EntityName contextEntityName);

        /// <summary>
        /// If doing material creation post processing, this causes the mesh resource to
        /// be created from the passed, world specific entity information.
        /// </summary>
        /// <param name="priority"></param>
        /// <param name="ent"></param>
        void CreateMaterialResource(float priority, IEntity ent, string materialName);

        /// <summary>
        /// Given an entity, recreate all the materials for this entity. Used when object
        /// initially created and when materials change
        /// </summary>
        /// <param name="priority"></param>
        /// <param name="ent"></param>
        void RebuildEntityMaterials(float priority, IEntity ent);

        /// <summary>
        /// Given an animation. Update the view of the entity with that animation. If teh
        /// entity is an avatar, the action will be different than if the entity is just
        /// a thing.
        /// </summary>
        /// <param name="priority"></param>
        /// <param name="ent"></param>
        /// <param name="sceneNodeName"></param>
        /// <param name="anim"></param>
        bool UpdateAnimation(float priority, IEntity ent, string sceneNodeName, ICmptAnimation anim);
    }
}
