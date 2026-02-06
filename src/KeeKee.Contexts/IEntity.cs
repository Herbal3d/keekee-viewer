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

namespace KeeKee.Contexts {
    /// <summary>
    /// EntityBase adds the handlers for the basic entity attributes and
    /// the management of the additional objects that subsystems can hang
    /// on an entity.
    /// </summary>
    public abstract class IEntity {

        public IKLogger EntityLogger { get; protected set; }

        public EntityClassifications Classification { get; protected set; }

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

        public IEntity(IKLogger pLog,
                          IWorld pWorld,
                          IRegionContext? pRContext,
                          IAssetContext pAContext,
                          EntityClassifications pClassification) {

            EntityLogger = pLog;
            WorldContext = pWorld;
            // The odd case of creating a region context (which is also an entity) requires passing in null
            //     for the region context and having the created entity knowing it is the context
            RegionContext = pRContext ?? this as IRegionContext
                ?? throw new ArgumentNullException("EntityBase: No RegionContext supplied and entity is not a region context");
            AssetContext = pAContext;
            Classification = pClassification;

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

        /// <summary>
        /// Try to get a component of the given type. This will look for exact
        /// matches first, then will look for derived types.
        /// This allows adding LLCmptLocation and looking it up as ICmptLocation.
        /// </summary>
        /// <param name="pType"></param>
        /// <param name="pComponent"></param>
        /// <returns></returns>
        private bool TryGetComponent(Type pType, out IEntityComponent pComponent) {
            lock (m_components) {
                if (m_components.TryGetValue(pType, out IEntityComponent? found)) {
                    pComponent = found;
                    return true;
                }

                foreach (var kvp in m_components) {
                    Type componentType = kvp.Value.GetType();
                    if (pType.IsAssignableFrom(componentType)) {
                        pComponent = kvp.Value;
                        return true;
                    }
                }
            }

            pComponent = null;
            return false;
        }

        public T Cmpt<T>() where T : class, IEntityComponent {
            if (TryGetComponent(typeof(T), out IEntityComponent cmpt)) {
                return (T)cmpt;
            }
            EntityLogger.Log(KLogLevel.DBADERROR, "EntityBase.Cmpt: No component of type {0}", typeof(T).ToString());
            throw new KeyNotFoundException("EntityBase.Cmpt: No component of type " + typeof(T).ToString());
        }

        public bool HasComponent<T>() where T : class, IEntityComponent {
            return TryGetComponent(typeof(T), out _);
        }
        // Test and return component if it exists
        public bool HasComponent<T>(out T? component) where T : class, IEntityComponent {
            if (TryGetComponent(typeof(T), out IEntityComponent? cmpt)) {
                component = (T)cmpt;
                return true;
            }
            component = null;
            return false;
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

        // Tell the entity that something about it changed
        virtual public void Update(UpdateCodes what) {
            EntityLogger.Log(KLogLevel.DUPDATEDETAIL, "IEntity.Update. what={0}", what);
        }
    }
}

