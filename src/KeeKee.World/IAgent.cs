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
using OMV = OpenMetaverse;

namespace KeeKee.World {

    public delegate void AgentUpdatedCallback(IAgent agnt, UpdateCodes what);

    /// <summary>
    /// The user acts on the world as an 'agent'. There is often an avatar
    /// associated with the agent (agent movement commands turn into movement
    /// of an avatar) but this is not required.
    /// </summary>
    public interface IAgent {
        event AgentUpdatedCallback OnAgentUpdated;

        // Not your normal update. Called when there have been authoritative updates to the
        //    information behind the agent. This feature is mosly used to do dead
        //    reconning (local position is different than server position)
        void DataUpdate(UpdateCodes what);

        // The entity (usually an avatar) associated with this agent
        // It will contain Components for movement, location, animation, etc.
        IEntity AssociatedEntity { get; }
        // the 

        // This is a call from the viewer telling the agent the camera has moved. The
        // agent can use this for anything it wishes but it's mostly used by data sources
        // to generate culling or update ordering.
        void UpdateCamera(OMV.Vector3d position, OMV.Quaternion direction, float far);

        // A number between 0..10 which give hints as to the user's interaction with the viewer.
        // Can be used by the agent to control update frequency and LOD.
        // Since stupid C# doesn't allow me to define constants in an interface
        //   definition, here are some values:
        // enum Interest {
        //     None = 0,     // user just doesn't care
        //     NoFocus = 2,  // viewer window does not have focus
        //     Idle = 6,     // user has been idle for a period
        //     Most = 10,    // user is active and doing stuff
        // };
        void UpdateInterest(int interest);
    }
}
