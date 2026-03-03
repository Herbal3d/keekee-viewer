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

using System.Net;

using KeeKee.Framework;
using KeeKee.Framework.Logging;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace KeeKee.Rest {

    public class RestHandlerFactory {
        private readonly IServiceProvider m_serviceProvider;

        public RestHandlerFactory(IServiceProvider pServiceProvider) {
            m_serviceProvider = pServiceProvider;
        }

        public RestHandler CreateHandler<T>(params object[] parameters) where T : RestHandler {
            return ActivatorUtilities.CreateInstance<T>(m_serviceProvider, parameters);
        }

        // Convenience method for creating a RestHandlerDisplayable with the required parameters.
        public RestHandlerDisplayable CreateHandlerDisplayable(string pPrefix, IDisplayable pDisplayableSource) {
            return (RestHandlerDisplayable)CreateHandler<RestHandlerDisplayable>(
                m_serviceProvider.GetRequiredService<KLogger<RestHandlerDisplayable>>(),
                m_serviceProvider.GetRequiredService<IOptions<RestManagerConfig>>(),
                m_serviceProvider.GetRequiredService<RestManager>(),
                pPrefix, pDisplayableSource);
        }
    }
}
