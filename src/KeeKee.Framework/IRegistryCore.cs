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

namespace KeeKee.Framework {
    /// <summary>
    /// An interface fetch pattern borrowed from OpenSim. Modules can register
    /// interfaces with someone implementing this interface and later get
    /// a handle to that interface.
    /// </summary>
    public interface IRegistryCore {
        void RegisterInterface<T>(T iface);
        bool TryGet<T>(out T iface);
        T Get<T>();

        void StackModuleInterface<M>(M mod);
        T[] RequestModuleInterfaces<T>();
        List<string> ModuleInterfaceTypeNames();
    }
}
