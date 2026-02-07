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

using Microsoft.Extensions.DependencyInjection;

using KeeKee.Contexts;
using KeeKee.Framework.Logging;

namespace KeeKee.Entity {

    /// <summary>
    /// The class that manages the creation of components.
    /// This is used to track what types of components are being created
    /// and to allow for future features like component pooling.
    /// </summary>
    public abstract class ComponentFactory {

        protected readonly IKLogger _log;
        protected readonly IServiceProvider _provider;

        protected Dictionary<Type, List<IEntityComponent>> _componentTypes = new Dictionary<Type, List<IEntityComponent>>();

        public ComponentFactory(IKLogger pLog,
                                IServiceProvider pProvider) {
            _log = pLog;
            _provider = pProvider;
        }

        public virtual T CreateComponent<T>(params object[] parameters) where T : class, IEntityComponent {
            var cmpt = ActivatorUtilities.CreateInstance<T>(_provider, parameters);

            // Keep track of the types of components being created. This is used for future features like component pooling.
            this.AddComponent<T>(cmpt);

            return cmpt;
        }

        public virtual void AddComponent<T>(IEntityComponent cmpt) where T : class, IEntityComponent {
            Type interfaceType = typeof(T);
            if (!_componentTypes.ContainsKey(interfaceType)) {
                _componentTypes[interfaceType] = new List<IEntityComponent>();
            }
            _componentTypes[interfaceType].Add(cmpt);
        }

        /// <summary>
        /// Get the types of components that have been created for a given component interface.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public virtual List<IEntityComponent> GetComponentTypes<T>() where T : class, IEntityComponent {
            Type cmptType = typeof(T);
            if (_componentTypes.ContainsKey(cmptType)) {
                return _componentTypes[cmptType];
            }
            foreach (var kvp in _componentTypes) {
                if (cmptType.IsAssignableFrom(kvp.Key)) {
                    return kvp.Value;
                }
            }
            return new List<IEntityComponent>();
        }
    }
}

