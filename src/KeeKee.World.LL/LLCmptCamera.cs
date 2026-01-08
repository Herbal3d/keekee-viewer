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
using KeeKee.Framework.Logging;
using OMV = OpenMetaverse;

namespace KeeKee.World.LL {
    public class LLCmptCamera : ICmptCamera {

        public IEntity ContainingEntity { get; private set; }

        private IKLogger m_log;
        private OMV.GridClient m_client;

        public OMV.Vector3 InitDirection { get; set; }

        public OMV.Quaternion Heading { get; set; }

        // true if the camera does not tilt side to side
        protected OMV.Vector3 m_yawFixedAxis = OMV.Vector3.UnitY;
        public bool YawFixed { get; set; }

        // rotate the camera by the given quaternion
        public void Rotate(OMV.Quaternion rot) {
            rot.Normalize();
            Heading = rot * Heading;
        }

        public void Rotate(OMV.Vector3 dir) {
            Rotate(dir.X, dir.Y, dir.Z);
        }

        /// <summary>
        /// rotate the specified amounts around the camera's local axis
        /// </summary>
        /// <param name="X"></param>
        /// <param name="Y"></param>
        /// <param name="Z"></param>
        public void Rotate(float X, float Y, float Z) {
            if (YawFixed) {
                // some of the rotation is around X
                OMV.Quaternion xvec = OMV.Quaternion.CreateFromAxisAngle(OMV.Vector3.UnitX, X);
                xvec.Normalize();
                // some of the rotation is around Z
                OMV.Quaternion zvec = OMV.Quaternion.CreateFromAxisAngle(OMV.Vector3.UnitZ, Z);
                zvec.Normalize();
                Heading = zvec * Heading;
                Heading = Heading * xvec;
            } else {
                OMV.Quaternion rot = new OMV.Quaternion(X, Y, Z);
                rot.Normalize();
                Rotate(rot);
            }
        }

        // public void lookAt(OMV.Vector3 target) {
        //     setDirection(target - m_position);
        // }

        public double Zoom { get; set; }

        public double Far { get; set; }

        public LLCmptCamera(IKLogger pLog,
                            IEntity pContainingEntity,
                            OMV.GridClient pClient
                            ) {
            m_log = pLog;
            m_client = pClient;
            ContainingEntity = pContainingEntity;

            InitDirection = new OMV.Vector3(0f, 1f, 0f);
            YawFixed = true;

            m_client.Self.Movement.Camera.Position = new OMV.Vector3(40f, 40f, 30f);
            pContainingEntity.Cmpt<ICmptLocation>().LocalPosition = new OMV.Vector3(40f, 40f, 30f);
            Heading = new OMV.Quaternion(0f, 1f, 0f);
        }

        public void Dispose() {
        }
    }
}

