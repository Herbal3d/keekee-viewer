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

using KeeKee.Framework.Logging;
using KeeKee.World;

using OMV = OpenMetaverse;

namespace KeeKee.Renderer.OGL {
    /// <summary>
    /// Class to do the "TargetOmega" fixed rotation.  Passed the parameters and does
    /// the rotation as the Process routine is called.
    /// </summary>
    public sealed class AnimatFixedRotation : AnimatBase {
        private IKLogger? m_Log;

        private uint m_infoID;
        private float m_rotationsPerSecond;
        private OMV.Vector3 m_rotationAxis;

        /// <summary>
        /// Create the animation. The passed animation block is expected
        /// to contain a defintion of a fixed rotation. If not, bad things will happen.
        /// </summary>
        /// <param name="anim">The IAnimation block with the info.</param>
        /// <param name="id">localID to lookup the prim in the RegionRenderInfo.renderPrimList</param>
        public AnimatFixedRotation(ICmptAnimation anim, uint id)
                        : base(AnimatBase.AnimatTypeFixedRotation) {
            m_infoID = id;
            if (anim.DoStaticRotation) {
                m_rotationsPerSecond = anim.StaticRotationRotPerSec;
                m_rotationAxis = anim.StaticRotationAxis;
            } else {
                // shouldn't get here
                m_rotationsPerSecond = 1;
                m_rotationAxis = OMV.Vector3.UnitX;
            }
        }

        /// <summary>
        /// Called for each frame. Advance the rotation.
        /// </summary>
        /// <param name="timeSinceLastFrame">seconds since last frame display</param>
        /// <param name="rri">RegionRenderInfo for region the animation is in</param>
        /// <returns>true to say we never exit</returns>
        public override bool Process(float timeSinceLastFrame, RegionRenderInfo rri) {
            float nextStep = m_rotationsPerSecond * timeSinceLastFrame;
            float nextIncrement = Constants.TWOPI * nextStep;
            while (nextIncrement > Constants.TWOPI) nextIncrement -= Constants.TWOPI;
            OMV.Quaternion newRotation = OMV.Quaternion.CreateFromAxisAngle(m_rotationAxis, nextIncrement);
            lock (rri) {
                try {
                    RenderablePrim rp = rri.renderPrimList[m_infoID];
                    rp.Rotation = newRotation * rp.Rotation;
                } catch (Exception e) {
                    m_Log?.Log(KLogLevel.DBADERROR, "Did not find prim for FixedRotation: {0}", e);
                }
            }
            return true;        // we never exit
        }
    }
}
