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

namespace KeeKee.Framework {
    /// <summary>
    /// A class that creates and instance of a DI registered class.
    /// To use, add InstanceFactory to the class creation invocation
    /// and then do a "_factory.Create<TheClassYouWant>()".
    /// </summary>
    public interface IInstanceFactory {
        T Create<T>() where T : class;
    }
    public class InstanceFactory : IInstanceFactory {

        private readonly IServiceProvider _provider;
        public InstanceFactory(IServiceProvider pProvider) {
            _provider = pProvider;
        }

        public T Create<T>() where T : class {
            return ActivatorUtilities.CreateInstance<T>(_provider);
        }
    }
}

