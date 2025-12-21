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
using KeeKee.Framework.Parameters;

namespace KeeKee.Framework.Modules {

    /// <summary>
    /// A standard interface for module loading.
    /// When a module is loaded, it constructor is called. This is for early
    /// initialization. After all modules are loaded, general, interdependent
    /// initialization is done when AfterAllModuelLoaded() is called.
    /// A module only starts working when Start() is called. Stop(), of course,
    /// says stop working. PrepareForUnload() allows late resource cleanup.
    /// </summary>
public interface IModule {
    string ModuleName { get ; }

    // void OnLoad(string name, IAppParameters parms);
    void OnLoad(string name, KeeKeeBase lgbase);

    bool AfterAllModulesLoaded();

    void Start();

    void Stop();

    bool PrepareForUnload();
}
}
