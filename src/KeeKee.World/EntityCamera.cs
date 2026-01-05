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
    public class EntityCamera : EntityBase {

        protected OMV.Vector3 m_initialDirection;
        public OMV.Vector3 InitDirection {
            get { return m_initialDirection; }
            set { m_initialDirection = value; }
        }

        // true if the camera does not tilt side to side
        protected OMV.Vector3 m_yawFixedAxis = OMV.Vector3.UnitY;
        protected bool m_yawFixed;
        public bool YawFixed {
            get { return m_yawFixed; }
            set { m_yawFixed = value; }
        }

        // rotate the camera by the given quaternion
        public void rotate(OMV.Quaternion rot) {
            rot.Normalize();
            m_heading = rot * m_heading;
        }

        public void rotate(OMV.Vector3 dir) {
            rotate(dir.X, dir.Y, dir.Z);
        }

        /// <summary>
        /// rotate the specified amounts around the camera's local axis
        /// </summary>
        /// <param name="X"></param>
        /// <param name="Y"></param>
        /// <param name="Z"></param>
        public void rotate(float X, float Y, float Z) {
            if (YawFixed) {
                // some of the rotation is around X
                OMV.Quaternion xvec = OMV.Quaternion.CreateFromAxisAngle(OMV.Vector3.UnitX, X);
                xvec.Normalize();
                // some of the rotation is around Z
                OMV.Quaternion zvec = OMV.Quaternion.CreateFromAxisAngle(OMV.Vector3.UnitZ, Z);
                zvec.Normalize();
                m_heading = zvec * m_heading;
                m_heading = m_heading * xvec;
            } else {
                OMV.Quaternion rot = new OMV.Quaternion(X, Y, Z);
                rot.Normalize();
                rotate(rot);
            }
        }

        // public void lookAt(OMV.Vector3 target) {
        //     setDirection(target - m_position);
        // }

        protected double m_zoom;
        public double Zoom { get { return m_zoom; } set { m_zoom = value; } }

        protected double m_far;
        public double Far { get { return m_far; } set { m_far = value; } }

        public EntityCamera(RegionContextBase pRContext, AssetContextBase pAContext)
                    : base(pRContext, pAContext) {
            m_yawFixed = true;
            m_globalPosition = new OMV.Vector3d(40f, 40f, 30f);
            m_heading = new OMV.Quaternion(0f, 1f, 0f);
        }

        public override void Dispose() {
            throw new NotImplementedException();
        }
    }
}
