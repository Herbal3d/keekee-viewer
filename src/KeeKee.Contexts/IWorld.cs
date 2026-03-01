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

using KeeKee.Framework;

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

    /// <summary>
    /// No one actually uses the IWorld interface other than World and most code
    /// references World directly since there is only one. But this defintiion
    /// exists to pull together the operations that can happen to the world.
    /// 
    /// The world is the central repository of objects that are received from
    /// the communcation stacks and that are displayed by the viewers.
    /// </summary>
    public interface IWorld : IDisplayable, IDisposable {

        #region Events
        // when a new region is being added to the world
        event WorldRegionNewCallback? OnWorldRegionNew;
        // when the underlying simulator is changing.
        event WorldRegionUpdatedCallback? OnWorldRegionUpdated;
        // when a new region is being removed from the world
        event WorldRegionRemovedCallback? OnWorldRegionRemoved;

        // when new items are added to the world
        event WorldEntityNewCallback? OnWorldEntityNew;
        // when a prim is updated
        event WorldEntityUpdateCallback? OnWorldEntityUpdate;
        // when an object is killed
        event WorldEntityRemovedCallback? OnWorldEntityRemoved;

        // when a new agent is added to the system
        event WorldAgentNewCallback? OnAgentNew;
        // when an agent is updated
        event WorldAgentUpdateCallback? OnAgentUpdate;
        // when an agent is removed from the world (logged out)
        event WorldAgentRemovedCallback? OnAgentRemoved;

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