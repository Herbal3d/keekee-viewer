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

using KeeKee.Contexts;

using OMV = OpenMetaverse;

namespace KeeKee.Renderer {

    public delegate void CameraControlUpdateCallback(CameraControl cam);

    public class CameraControl {

        public event CameraControlUpdateCallback? OnCameraUpdate;

        public IEntity? AssociatedAgent { get; set; }

        public CameraControl() {
            m_heading = new OMV.Quaternion(OMV.Vector3.UnitY, 0f);
            m_globalPosition = new OMV.Vector3d(0d, 20d, 30d);   // World coordinates (Z up)
            m_zoom = 1.0f;
            m_far = 300.0f;
            m_yawFixed = true;
        }

        // Global position of the camera
        protected OMV.Vector3d m_globalPosition;
        public OMV.Vector3d GlobalPosition {
            get { return m_globalPosition; }
            set {
                bool changed = (m_globalPosition != value);
                m_globalPosition = value;
                if (changed && (OnCameraUpdate != null)) OnCameraUpdate(this);
            }
        }

        protected OMV.Quaternion m_heading;
        public OMV.Quaternion Heading {
            get { return m_heading; }
            set {
                bool changed = (m_heading != value);
                m_heading = value;
                if (changed && (OnCameraUpdate != null)) OnCameraUpdate(this);
            }
        }

        /// <summary>
        /// Update both position and heading with one call. Remember that the position
        /// is a global position.
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="heading"></param>
        public void Update(OMV.Vector3d pos, OMV.Quaternion heading) {
            bool changed = (m_heading != heading) | (m_globalPosition != pos);
            m_globalPosition = pos;
            m_heading = heading;
            if (changed && (OnCameraUpdate != null)) OnCameraUpdate(this);
        }

        protected float m_zoom;
        public float Zoom {
            get { return m_zoom; }
            set {
                bool changed = (m_zoom != value);
                m_zoom = value;
                if (changed && (OnCameraUpdate != null)) OnCameraUpdate(this);
            }
        }

        protected float m_far;
        public float Far {
            get { return m_far; }
            set {
                bool changed = (m_far != value);
                m_far = value;
                if (changed && (OnCameraUpdate != null)) OnCameraUpdate(this);
            }
        }

        // true if the camera does not tilt side to side
        protected OMV.Vector3 m_yawFixedAxis = OMV.Vector3.UnitY;
        protected bool m_yawFixed;
        public bool YawFixed { get { return m_yawFixed; } set { m_yawFixed = value; } }

        // rotate the camera by the given quaternion
        public void rotate(OMV.Quaternion rot) {
            rot.Normalize();
            m_heading = rot * m_heading;
            if (OnCameraUpdate != null) OnCameraUpdate(this);
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
                // some of the rotation is around local X
                OMV.Quaternion xvec = OMV.Quaternion.CreateFromAxisAngle(OMV.Vector3.UnitY, -X);
                // some of the rotation is around Z
                OMV.Quaternion zvec = OMV.Quaternion.CreateFromAxisAngle(OMV.Vector3.UnitZ, Z);
                // m_heading = m_heading * xvec;
                m_heading = xvec * m_heading * zvec;
                // LogManager.Log.Log(LogLevel.DVIEWDETAIL, "CameraControl.rotate: xv={0}, zv={1}, h={2}",
                //                     xvec, zvec, m_heading);
            } else {
                OMV.Quaternion rot = new OMV.Quaternion(X, Y, Z);
                rot.Normalize();
                rotate(rot);
            }
            if (OnCameraUpdate != null) OnCameraUpdate(this);
        }
    }
}
