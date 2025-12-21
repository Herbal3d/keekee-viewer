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

namespace KeeKee.Framework.Parameters {
/// <summary>
/// The application handles parameters with several layers of
/// parameters specification.
/// The application modules will add Default parameters. Then
/// parameters come from the configuratin file ("Ini" parameters).
/// Then the user specific parameters are added and finally
/// override, session parameters which usually come from the invocation
/// line.
/// When searching for a value for a parameter, these sets are searched
/// in the order: override, user, ini, default. The first found value is
/// used.
/// 
/// Parameters are stored in multiple lists. Default, Ini, User, and Override.
/// Default comes from the program itself (default values for every
/// possible parameter), Ini is read from the initialization file,
/// User is read from the user parameter file and Override are command
/// line parameters.
/// 
/// These lists are searched in order from Override to Default. The first
/// found value is used. The exeception is the multiple valued parameters
/// which can only occur in the Ini list.
/// 
/// The workflow for parameters is:
/// <list type="bullet">
/// <item>
/// create the instance of the class providing IParameterProvider;
/// </item>
/// <item>
/// as each extension or module is initialized, it adds the defaults
/// for its parameters with 'addDefaultParameter'. This makes all the
/// parameters, their values and documentation available;
/// </item>
/// <item>
/// read in the User file
/// </item>
/// <item>
/// read in the Ini file
/// </item>
/// <item>
/// </item>
/// read in the Modules file (into the Ini list)
/// <item>
/// add any override name/value pairs with 'addOverrideParameter';
/// use the parameters
/// <item>
/// </list>
/// </summary>
 
public interface IAppParameters : IParameters {
    /// <summary>
    /// Add a parameter to the Default store. Searched last.
    /// </summary>
    /// <param name="key">parameter name</param>
    /// <param name="value">string representation of value</param>
    /// <param name="desc">human readable explanation of what the parameter does</param>
    void AddDefaultParameter(string key, string value, string desc);

    /// <summary>
    /// Add a parameter to the Ini store. Searched next to last.
    /// </summary>
    /// <param name="key">parameter name</param>
    /// <param name="value">string representation of value</param>
    /// <param name="desc">human readable explanation of what the parameter does.
    /// Value may be 'null' to specify no description</param>
    void AddIniParameter(string key, string value, string desc);

    /// <summary>
    /// Add a parameter to the User store. Searched second.
    /// </summary>
    /// <param name="key">parameter name</param>
    /// <param name="value">string representation of value</param>
    /// <param name="desc">human readable explanation of what the parameter does.
    /// Value may be 'null' to specify no description</param>
    void AddUserParameter(string key, string value, string desc);

    /// <summary>
    /// Add an override parameter to the store. Searched first
    /// </summary>
    /// <param name="key">parameter name</param>
    /// <param name="value">string representation of value</param>
    void AddOverrideParameter(string key, string value);

}
}
