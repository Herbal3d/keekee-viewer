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
using KeeKee.Framework.Logging;

using OMV = OpenMetaverse;

namespace KeeKee.World {
    /// <summary>
    /// EntityBase adds the handlers for the basic entity attributes and
    /// the management of the additional objects that subsystems can hang
    /// on an entity.
    /// </summary>
    public abstract class EntityBase : IEntity {

        public IKLogger EntityLogger { get; protected set; }

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
        public IWorld WorldContext { get; protected set; }
        public IRegionContext RegionContext { get; protected set; }
        public IAssetContext AssetContext { get; protected set; }

        // Every entity has a name
        public virtual EntityName Name { get; set; }

        public virtual IEntity? ContainingEntity { get; set; } = null;

        public BHash LastEntityHashCode { get; set; } = new BHashULong(0);

        protected Dictionary<Type, IEntityComponent> m_components = new Dictionary<Type, IEntityComponent>();

        public EntityBase(IKLogger pLog,
                          IWorld pWorld,
                          IRegionContext? pRContext,
                          IAssetContext pAContext) {

            EntityLogger = pLog;
            WorldContext = pWorld;
            // The odd case of creating a region context requires passing in null
            //     for the region context and having the created entity knowing it is the context
            RegionContext = pRContext ?? this as IRegionContext
                ?? throw new ArgumentNullException("EntityBase: No RegionContext supplied and entity is not a region context");
            AssetContext = pAContext;

            m_LGID = NextLGID();
            Name = new EntityName(AssetContext, LGID.ToString());
        }

        #region Component Management
        /// <summary>
        /// Register an Module interface.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="iface"></param>
        public void AddComponent<T>(T pComponent) where T : class, IEntityComponent {
            lock (m_components) {
                if (!m_components.ContainsKey(typeof(T))) {
                    m_components.Add(typeof(T), pComponent);
                }
            }
        }

        public T Cmpt<T>() where T : class, IEntityComponent {
            if (m_components.ContainsKey(typeof(T))) {
                IEntityComponent cmpt = m_components[typeof(T)];
                return (T)cmpt;
            }
            EntityLogger.Log(KLogLevel.DBADERROR, "EntityBase.Cmpt: No component of type {0}", typeof(T).ToString());
            throw new KeyNotFoundException("EntityBase.Cmpt: No component of type " + typeof(T).ToString());
        }

        public bool HasComponent<T>() where T : class, IEntityComponent {
            return m_components.ContainsKey(typeof(T));
        }
        public bool HasComponent<T>(out T? component) where T : class, IEntityComponent {
            bool ret = m_components.ContainsKey(typeof(T));
            if (ret) {
                IEntityComponent cmpt = m_components[typeof(T)];
                component = (T)cmpt;
            } else {
                component = null;
            }
            return ret;
        }
        #endregion Component Management

        public virtual void Dispose() {
            // tell all the interfaces we're done with them
            foreach (var kvp in m_components) {
                try {
                    IDisposable idis = (IDisposable)kvp.Value;
                    // is this right? How to tell object it's done here but don't need to zap oneself
                    // idis.Dispose();
                } catch {
                    // if it won't dispose it's not our problem
                }
            }
            m_components.Clear();
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

        // Tell the entity that something about it changed
        virtual public void Update(UpdateCodes what) {
            EntityLogger.Log(KLogLevel.DUPDATEDETAIL, "EntityBase.Update calling RegionContext.UpdateEntity. w={0}", what);
        }
    }
}
