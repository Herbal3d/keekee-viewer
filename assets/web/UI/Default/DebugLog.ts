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

// Log debug message to the DEBUGG div if it exists
// If classname is specified, add that class to the new div
export function LogDebug(msg: string, classname?: string) : void {
    const debugg = document.getElementById("k-DEBUGG");
    if (debugg != undefined) {
        const newline = document.createElement("div");
        newline.appendChild(document.createTextNode(msg));
        if (classname != undefined) {
            newline.setAttribute("class", classname);
        }
        debugg.appendChild(newline);
        if (debugg.childElementCount > 20) {
            debugg.removeChild(debugg.firstChild as ChildNode);
        }
    }
}

