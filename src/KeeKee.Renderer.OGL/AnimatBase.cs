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
using KeeKee.Framework;
using KeeKee.World;

namespace KeeKee.Renderer.OGL {
    public class AnimatBase {
        public int AnimatType;
        public const int AnimatTypeAny             = 0;
        public const int AnimatTypeFixedRotation   = 1;
        public const int AnimatTypeRotation        = 2;
        public const int AnimatTypePosition        = 3;

        public AnimatBase(int type) {
            AnimatType = type;
        }

        public virtual bool Process(float timeSinceLastFrame, RegionRenderInfo rri) {
            return false;   // false saying to delete
        }

        /// <summary>
        /// Loop through the list of animations for this region and call their "Process" routines
        /// </summary>
        /// <param name="timeSinceLastFrame">seconds since list frame</param>
        /// <param name="rri">RegionRenderInfo for the region</param>
        public static void ProcessAnimations(float timeSinceLastFrame, RegionRenderInfo rri) {
            lock (rri) {
                List<AnimatBase> removeAnimations = new List<AnimatBase>();
                foreach (AnimatBase ab in rri.animations) {
                    try {
                        if (!ab.Process(timeSinceLastFrame, rri)) {
                            removeAnimations.Add(ab);   // remember so we can remove later
                        }
                    }
                    catch {
                    }
                }
                // since we can't remove animations while interating the list, do it now
                foreach (AnimatBase ab in removeAnimations) {
                    rri.animations.Remove(ab);
                }
            }
        }

        /// <summary>
        /// Given an IAnimation instance (passed from comm or world), build the animation
        /// type needed.
        /// </summary>
        /// <param name="anim"></param>
        /// <param name="id">localID of prim that looks up in RegionRenderInfo.renderPrimList</param>
        /// <returns></returns>
        public static AnimatBase CreateAnimation(IAnimation anim, uint id) {
            if (anim.DoStaticRotation) {
                // the only programmable animation we know how to do is fixed axis rotation
                return new AnimatFixedRotation(anim, id);
            }
            // default is an animation that will just exit when used
            return new AnimatBase(AnimatBase.AnimatTypeFixedRotation);
        }

    }
}
