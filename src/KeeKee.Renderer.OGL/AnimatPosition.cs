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
using KeeKee.Framework.Logging;
using KeeKee.World;
using OMV = OpenMetaverse;

namespace KeeKee.Renderer.OGL {
    /// <summary>
    /// Class to advance a position update.
    /// </summary>
    public sealed class AnimatPosition : AnimatBase {
        private uint m_infoID;
        private OMV.Vector3 m_origionalPosition;
        private OMV.Vector3 m_targetPosition;
        private float m_durationSeconds;
        private OMV.Vector3 m_distanceVector;
        private float m_progress;

        /// <summary>
        /// Create the animation. The passed animation block is expected
        /// to contain a defintion of a fixed rotation. If not, bad things will happen.
        /// </summary>
        /// <param name="anim">The IAnimation block with the info.</param>
        /// <param name="id">localID to lookup the prim in the RegionRenderInfo.renderPrimList</param>
        public AnimatPosition(OMV.Vector3 newPos, float durationSeconds, RegionRenderInfo rri, uint id)
                        : base(AnimatBase.AnimatTypePosition) {
            m_infoID = id;
            RenderablePrim rp = rri.renderPrimList[id];
            m_origionalPosition = rp.Position;
            m_targetPosition = newPos;
            m_durationSeconds = durationSeconds;
            m_distanceVector = m_targetPosition - m_origionalPosition;
            m_progress = 0f;
        }

        /// <summary>
        /// Called for each frame. Advance the position.
        /// </summary>
        /// <param name="timeSinceLastFrame">seconds since last frame display</param>
        /// <param name="rri">RegionRenderInfo for region the animation is in</param>
        /// <returns>true to say we never exit</returns>
        public override bool Process(float timeSinceLastFrame, RegionRenderInfo rri) {
            bool ret = true;
            float thisProgress = timeSinceLastFrame / m_durationSeconds;
            m_progress += thisProgress;
            RenderablePrim rp = rri.renderPrimList[m_infoID];
            if (m_progress >= 1f) {
                // if progressed all the way, we're at the destination
                rp.Position = m_targetPosition;
                ret = false;    // we're done animating
            }
            else {
                // only part way there
                rp.Position = m_origionalPosition + m_distanceVector * m_progress;
            }
            return ret;
        }
    }
}
