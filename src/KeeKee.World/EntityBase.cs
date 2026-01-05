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

using KeeKee.Framework.Logging;

using OMV = OpenMetaverse;

namespace KeeKee.World {
    /// <summary>
    /// EntityBase adds the handlers for the basic entity attributes and
    /// the management of the additional objects that subsystems can hang
    /// on an entity.
    /// </summary>
    public abstract class EntityBase : IEntity {

        private IKLogger m_log;

        // Every entity has a local, session scoped ID
        protected ulong m_LGID = 0;
        public ulong LGID {
            get {
                if (m_LGID == 0) m_LGID = NextLGID();
                return m_LGID;
            }
        }
        private static ulong m_LGIDIndex = 0x10000000;
        public static ulong NextLGID() { return m_LGIDIndex++; }

        // Contexts for this entity
        public IWorld WorldContext { get; private set; }
        public IRegionContext RegionContext { get; private set; }
        public IAssetContext AssetContext { get; private set; }

        // Every entity has a name
        public virtual EntityName Name { get; set; }

        public virtual IEntity? ContainingEntity { get; set; } = null;

        // If associated with a parent, go to the parent and remove us from
        // the parent's container.
        // Call before removing/deleting/destroying an entity.
        public virtual void DisconnectFromContainer() {
            if (ContainingEntity != null) {
                IEntityCollection coll;
                if (ContainingEntity.TryGet<IEntityCollection>(out coll)) {
                    coll.RemoveEntity(this);
                }
                ContainingEntity = null;
            }
        }
        protected IEntityCollection? m_entityCollection = null;
        public virtual void AddEntityToContainer(IEntity ent) {
            if (m_entityCollection == null) {
                m_entityCollection = new EntityCollection(this.Name.Name);
            }
            m_entityCollection.AddEntity(ent);
        }
        public virtual void RemoveEntityFromContainer(IEntity ent) {
            if (m_entityCollection != null) {
                m_entityCollection.RemoveEntity(ent);
                if (m_entityCollection.Count == 0) {
                    m_entityCollection = null;
                }
            }
        }

        protected int m_lastEntityHashCode = 0;
        public int LastEntityHashCode { get { return m_lastEntityHashCode; } set { m_lastEntityHashCode = value; } }

        static EntityBase() {
            AdditionSubsystems = new Dictionary<string, int>();
        }

        public EntityBase(IKLogger pLog,
                          IWorld pWorld,
                          IRegionContext pRContext,
                          IAssetContext pAContext) {

            m_log = pLog;
            WorldContext = pWorld;
            RegionContext = pRContext;
            AssetContext = pAContext;

            m_LGID = NextLGID();
            Name = new EntityName(AssetContext, LGID.ToString());

            Additions = new Object[EntityBase.ADDITIONCLASSES];
            for (int ii = 0; ii < EntityBase.ADDITIONCLASSES; ii++) {
                Additions[ii] = null;
            }
        }


        #region IRegistryCore
        protected Dictionary<Type, object> m_moduleInterfaces = new Dictionary<Type, object>();

        /// <summary>
        /// Register an Module interface.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="iface"></param>
        public void RegisterInterface<T>(T iface) {
            lock (m_moduleInterfaces) {
                if (!m_moduleInterfaces.ContainsKey(typeof(T))) {
                    m_moduleInterfaces.Add(typeof(T), iface);
                }
            }
        }

        public bool TryGet<T>(out T iface) {
            if (m_moduleInterfaces.ContainsKey(typeof(T))) {
                iface = (T)m_moduleInterfaces[typeof(T)];
                return true;
            }
            iface = default(T);
            return false;
        }

        public T Get<T>() {
            if (m_moduleInterfaces.ContainsKey(typeof(T))) {
                return (T)m_moduleInterfaces[typeof(T)];
            }
            return default(T);
        }

        public void StackModuleInterface<M>(M mod) {
        }

        public T[] RequestModuleInterfaces<T>() {
            return new T[] { default(T) };
        }

        public List<string> ModuleInterfaceTypeNames() {
            List<string> ret = new List<string>();
            foreach (Type k in m_moduleInterfaces.Keys) {
                ret.Add(k.ToString());
            }
            return ret;
        }
        #endregion IRegistryCore

        public virtual void Dispose() {
            // tell all the interfaces we're done with them
            foreach (KeyValuePair<Type, object> kvp in m_moduleInterfaces) {
                try {
                    IDisposable idis = (IDisposable)kvp.Value;
                    // is this right? How to tell object it's done here but don't need to zap oneself
                    // idis.Dispose();
                } catch {
                    // if it won't dispose it's not our problem
                }
            }
            m_moduleInterfaces.Clear();

            // clean out references in the additions table
            for (int ii = 0; ii < Additions.Length; ii++) {
                try {
                    IDisposable idisp = (IDisposable)Additions[ii];
                } catch {
                } finally {
                    Additions[ii] = null;
                }
            }
        }

        #region LOCATION
        protected OMV.Quaternion m_heading = new OMV.Quaternion();
        virtual public OMV.Quaternion Heading {
            get {
                return m_heading;
            }
            set {
                m_heading = value;
            }
        }

        virtual public OMV.Vector3 RegionPosition {
            get {
                if (this.ContainingEntity == null) {
                    return this.LocalPosition;
                } else {
                    // LogManager.Log.Log(LogLevel.DWORLDDETAIL, "EntityBase.RegionPosition: {0} relative to {1}",
                    //     this.LocalPosition, this.ContainingEntity.RegionPosition);
                    return this.LocalPosition;
                    // DEBUG: the following causes objects to drift off into space. Not sure why.
                    // return this.LocalPosition + this.ContainingEntity.RegionPosition;
                }
            }
        }

        // local coodinate position relative to a parent (if exists)
        protected OMV.Vector3 m_localPosition = new OMV.Vector3(0f, 0f, 0f);
        virtual public OMV.Vector3 LocalPosition {
            get {
                return m_localPosition;
            }
            set {
                m_localPosition = value;
            }
        }

        protected OMV.Vector3d m_globalPosition;
        virtual public OMV.Vector3d GlobalPosition {
            get {
                OMV.Vector3 regionRelative = this.RegionPosition;
                if (RegionContext != null) {
                    return new OMV.Vector3d(
                        RegionContext.WorldBase.X + (double)regionRelative.X,
                        RegionContext.WorldBase.Y + (double)regionRelative.Y,
                        RegionContext.WorldBase.Z + (double)regionRelative.Z);
                }
                return new OMV.Vector3d(20d, 20d, 20d);
            }
        }
        #endregion LOCATION


        #region ADDITIONS
        const int ADDITIONCLASSES = 7;  // maximum subsystems that can be added
        public Object[] Additions;
        public static Dictionary<string, int> AdditionSubsystems;

        public Object Addition(int ii) {
            if (ii < Additions.Length) return Additions[ii];
            else return null;
        }

        public Object Addition(string ss) {
            if (AdditionSubsystems.ContainsKey(ss)) return Additions[EntityBase.AdditionSubsystems[ss]];
            else return null;
        }
        public void SetAddition(int ii, Object obj) { Additions[ii] = obj; }

        /// <summary>
        /// Create a new subsystem index. If teh subsystem is already
        /// defined, the previously allocated index is returned.
        /// </summary>
        /// <param name="addClass">Name of the subsystem to add</param>
        /// <returns>The newly allocated index or the previously allocated
        /// index for this subsystem.</returns>
        public static int AddAdditionSubsystem(string addClass) {
            int ret = 0;
            if (AdditionSubsystems.ContainsKey(addClass)) {
                // it's already in the list, just return the old number
                ret = AdditionSubsystems[addClass];
            } else {
                int newIndex = 0;
                foreach (KeyValuePair<string, int> kvp in AdditionSubsystems) {
                    if (kvp.Value >= newIndex) newIndex = kvp.Value + 1;
                }
                AdditionSubsystems.Add(addClass, newIndex);
                ret = newIndex;
                // make sure the addition class array is big enough for the new class
                if (ADDITIONCLASSES <= newIndex) {
                    // We cannot add more than the max!!
                    throw new KeeKeeException("Adding more Entity object classes than allowed. Tried to add " + addClass);
                    // Object[] newAdditions = new Object[Additions.Length + 4];
                    // for (int ii = 0; ii < newAdditions.Length; ii++) {
                    //     newAdditions[ii] = ii >= Additions.Length ? null : Additions[ii];
                    // }
                    // Additions = newAdditions;
                }
            }
            return ret;
        }
        #endregion ADDITIONS

        // Tell the entity that something about it changed
        virtual public void Update(UpdateCodes what) {
            if (this.RegionContext != null) {
                m_log.Log(KLogLevel.DUPDATEDETAIL, "EntityBase.Update calling RegionContext.UpdateEntity. w={0}", what);
                IEntityCollection coll;
                if (this.RegionContext.TryGet<IEntityCollection>(out coll)) {
                    coll.UpdateEntity(this, what);
                }
            }
            return;
        }
    }
}
