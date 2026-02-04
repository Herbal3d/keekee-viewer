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

namespace KeeKee.Contexts {

    public delegate void WorldRegionNewCallback(IRegionContext rcontext);
    public delegate void WorldRegionUpdatedCallback(IRegionContext rcontext, UpdateCodes what);
    public delegate void WorldRegionRemovedCallback(IRegionContext rcontext);

    public delegate void WorldEntityNewCallback(IEntity ent);
    public delegate void WorldEntityUpdateCallback(IEntity ent, UpdateCodes what);
    public delegate void WorldEntityRemovedCallback(IEntity ent);

    public delegate void WorldAgentNewCallback(IEntity agnt);
    public delegate void WorldAgentUpdateCallback(IEntity agnt, UpdateCodes what);
    public delegate void WorldAgentRemovedCallback(IEntity agnt);

    public delegate IEntity WorldCreateEntityCallback();
    public delegate IEntity WorldCreateAvatarCallback();

    public enum WorldGroupCode {
        LLWorld,
        OtherWorld,
    }

    public enum UpdateCodes : uint {
        None = 0,
        AttachmentPoint = 1 << 0,
        Material = 1 << 1,
        ClickAction = 1 << 2,
        Scale = 1 << 3,
        ParentID = 1 << 4,
        PrimFlags = 1 << 5,
        PrimData = 1 << 6,
        MediaURL = 1 << 7,
        ScratchPad = 1 << 8,
        Textures = 1 << 9,
        TextureAnim = 1 << 10,
        NameValue = 1 << 11,
        Position = 1 << 12,
        Rotation = 1 << 13,
        Velocity = 1 << 14,
        Acceleration = 1 << 15,
        AngularVelocity = 1 << 16,
        CollisionPlane = 1 << 17,
        Text = 1 << 18,
        Particles = 1 << 19,
        ExtraData = 1 << 20,
        Sound = 1 << 21,
        Joint = 1 << 22,
        Terrain = 1 << 23,
        Focus = 1 << 24,
        Light = 1 << 25,
        Animation = 1 << 26,
        Appearance = 1 << 27,
        New = 1 << 30,  // a new item
        FullUpdate = 0x0fffffff
    }

    /// <summary>
    /// No one actually uses the IWorld interface other than World and most code
    /// references World directly since there is only one. But this defintiion
    /// exists to pull together the operations that can happen to the world.
    /// 
    /// The world is the central repository of objects that are received from
    /// the communcation stacks and that are displayed by the viewers.
    /// </summary>
    public interface IWorld {

        #region Events
        // when a new region is being added to the world
        event WorldRegionNewCallback OnWorldRegionNew;
        // when the underlying simulator is changing.
        event WorldRegionUpdatedCallback OnWorldRegionUpdated;
        // when a new region is being removed from the world
        event WorldRegionRemovedCallback OnWorldRegionRemoved;

        // when new items are added to the world
        event WorldEntityNewCallback OnWorldEntityNew;
        // when a prim is updated
        event WorldEntityUpdateCallback OnWorldEntityUpdate;
        // when an object is killed
        event WorldEntityRemovedCallback OnWorldEntityRemoved;

        // when a new agent is added to the system
        event WorldAgentNewCallback OnAgentNew;
        // when an agent is updated
        event WorldAgentUpdateCallback OnAgentUpdate;
        // when an agent is removed from the world (logged out)
        event WorldAgentRemovedCallback OnAgentRemoved;

        #endregion Events

        // REGION MANAGEMENT
        void AddRegion(IRegionContext rcontext);
        void RemoveRegion(IRegionContext rcontext);
        IRegionContext? GetRegion(EntityName name);
        IRegionContext? FindRegion(Predicate<IRegionContext> pred);

        // ENTITY MANAGEMENT
        // A global request for an entity. Used by renderer because it looses context
        // when called back from the depths of rendering.
        bool TryGetEntity(EntityName entName, out IEntity? ent);

        // AGENT MANAGEMENT
        void AddAgent(IEntity agnt);
        void RemoveAgent();
        IEntity? Agent { get; }

    }
}