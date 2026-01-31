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
using System.Text;
using KeeKee.Framework.Logging;
using KeeKee.Framework.WorkQueue;
using Microsoft.Extensions.DependencyInjection;
using OMV = OpenMetaverse;

namespace KeeKee.World {
    public class EntityCollection : IEntityCollection {
        protected KLogger<EntityCollection> m_log;

        public event EntityNewCallback? OnEntityNew;
        public event EntityUpdateCallback? OnEntityUpdate;
        public event EntityRemovedCallback? OnEntityRemoved;

        static bool m_shouldQueueEvent = true;
        static BasicWorkQueue m_workQueueEvent;

        protected OMV.DoubleDictionary<string, ulong, IEntity> m_entityDictionary;

        public EntityCollection(KLogger<EntityCollection> pLog,
                                WorkQueueManager pQueueManager) {
            m_log = pLog;
            m_workQueueEvent = pQueueManager.CreateBasicWorkQueue("EntityCollectionWorkQueue");
            m_entityDictionary = new OMV.DoubleDictionary<string, ulong, IEntity>();
        }

        public int Count {
            get { return m_entityDictionary.Count; }
        }

        public void AddEntity(IEntity entity) {
            // m_log.Log(KLogLevel.DWORLDDETAIL, "AddEntity: {0}, n={1}", m_name, entity.Name.Name);
            if (TrackEntity(entity)) {
                // tell the viewer about this prim and let the renderer convert it
                //    into the format needed for display
                if (m_shouldQueueEvent) {
                    // disconnect this work from the caller -- use another thread
                    m_workQueueEvent.DoLater(DoEventLater, entity);
                } else {
                    if (OnEntityNew != null) OnEntityNew(entity);
                }
            }
        }

        private bool DoEventLater(DoLaterJob qInstance, object parm) {
            EntityNewCallback? enc = OnEntityNew;
            if (enc != null) {
                enc.Invoke((IEntity)parm);
            }
            return true;
        }

        public void UpdateEntity(IEntity entity, UpdateCodes detail) {
            m_log.Log(KLogLevel.DUPDATEDETAIL, "UpdateEntity: " + entity.Name);
            if (m_shouldQueueEvent) {
                object[] parms = { entity, detail };
                m_workQueueEvent.DoLater(DoUpdateLater, parms);
            } else {
                if (OnEntityUpdate != null) OnEntityUpdate(entity, detail);
            }
        }

        private bool DoUpdateLater(DoLaterJob qInstance, object parm) {
            object[] parms = (object[])parm;
            IEntity ent = (IEntity)parms[0];
            UpdateCodes detail = (UpdateCodes)parms[1];
            EntityUpdateCallback? euc = OnEntityUpdate;
            if (euc != null) {
                euc.Invoke(ent, detail);
            }
            return true;
        }

        public void RemoveEntity(IEntity entity) {
            m_log.Log(KLogLevel.DWORLDDETAIL, "RemoveEntity: " + entity.Name);

            EntityRemovedCallback? erc = OnEntityRemoved;
            if (erc != null) erc.Invoke(entity);

            lock (this) {
                m_entityDictionary.Remove(entity.Name.Name);
            }
        }

        private void SelectEntity(IEntity ent) {
        }

        private bool TrackEntity(IEntity ent) {
            try {
                lock (this) {
                    if (m_entityDictionary.ContainsKey(ent.Name.Name)) {
                        m_log.Log(KLogLevel.DWORLD, "Asked to add same entity again: " + ent.Name);
                    } else {
                        m_entityDictionary.Add(ent.Name.Name, ent.LGID, ent);
                        return true;
                    }
                }
            } catch {
                // sometimes they send me the same entry twice
                m_log.Log(KLogLevel.DWORLD, "Asked to add same entity again: " + ent.Name);
            }
            return false;
        }

        private void UnTrackEntity(IEntity ent) {
            m_entityDictionary.Remove(ent.Name.Name, ent.LGID);
        }

        private void ClearTrackedEntities() {
            m_entityDictionary.Clear();
        }
        public bool TryGetEntity(ulong lgid, out IEntity ent) {
            return m_entityDictionary.TryGetValue(lgid, out ent);
        }

        public bool TryGetEntity(string entName, out IEntity ent) {
            return m_entityDictionary.TryGetValue(entName, out ent);
        }

        public bool TryGetEntity(EntityName entName, out IEntity ent) {
            return m_entityDictionary.TryGetValue(entName.Name, out ent);
        }

        /// <summary>
        /// </summary>
        /// <param name="localID"></param>
        /// <param name="ent"></param>
        /// <param name="createIt"></param>
        /// <returns>true if we created a new entry</returns>
        public bool TryGetCreateEntity(EntityName entName, out IEntity? ent, RegionCreateEntityCallback createIt) {
            // m_log.Log(LogLevel.DWORLDDETAIL, "TryGetCreateEntity: n={0}", entName);
            try {
                lock (this) {
                    if (!TryGetEntity(entName, out ent)) {
                        IEntity newEntity = createIt();
                        AddEntity(newEntity);
                        ent = newEntity;
                    }
                }
                return true;
            } catch (Exception e) {
                m_log.Log(KLogLevel.DBADERROR, "TryGetCreateEntityLocalID: Failed to create entity: {0}", e.ToString());
            }
            ent = null;
            return false;
        }

        public IEntity FindEntity(Predicate<IEntity> pred) {
            return m_entityDictionary.FindValue(pred);
        }

        public void ForEach(Action<IEntity> act) {
            lock (this) {
                m_entityDictionary.ForEach(act);
            }
        }

        public void Dispose() {
            // TODO: do something about the entity list
            m_entityDictionary.ForEach(delegate (IEntity ent) {
                ent.Dispose();
            });
            m_entityDictionary.Clear(); // release any entities we might have

        }
    }
}
