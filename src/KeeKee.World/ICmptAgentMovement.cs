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

namespace KeeKee.World {

    /// <summary>
    /// The user acts on the world as an 'agent'. There is often an avatar
    /// associated with the agent (agent movement commands turn into movement
    /// of an avatar) but this is not required.
    /// </summary>
    public interface ICmptAgentMovement : IEntityComponent {

        // This also updates the agent's representation in the world (usually an avatar)
        // TODO: this is just enough to get display working. Figure out better movement model
        void MoveForward(bool startstop);
        void MoveBackward(bool startstop);
        void MoveUp(bool startstop);
        void MoveDown(bool startstop);
        void TurnLeft(bool startstop);
        void TurnRight(bool startstop);
        void Fly(bool startstop);
        void StopAllMovement();
    }
}