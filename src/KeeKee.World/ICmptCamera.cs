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
    public interface ICmptCamera : IEntityComponent {

        public OMV.Vector3 InitDirection { get; set; }

        // true if the camera does not tilt side to side
        public bool YawFixed { get; set; }

        // rotate the camera by the given quaternion
        public void Rotate(OMV.Quaternion rot);

        public void Rotate(OMV.Vector3 dir);

        /// <summary>
        /// rotate the specified amounts around the camera's local axis
        /// </summary>
        /// <param name="X"></param>
        /// <param name="Y"></param>
        /// <param name="Z"></param>
        public void Rotate(float X, float Y, float Z);

        // public void lookAt(OMV.Vector3 target) {
        //     setDirection(target - m_position);
        // }

        public double Zoom { get; set; }

        public double Far { get; set; }
    }
}

