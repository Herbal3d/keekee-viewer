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
using OMV = OpenMetaverse;

namespace KeeKee.World.LL {
public class LLAnimation : IAnimation {

    // for the moment, there is not much to an animation
    private OMV.Vector3 m_angularVelocity = OMV.Vector3.Zero;
    public OMV.Vector3 AngularVelocity {
        get { return m_angularVelocity; }
        set { m_angularVelocity = value; }
    }

    private bool m_doStaticRotation = false;
    public bool DoStaticRotation {
        get { return m_doStaticRotation; }
        set { m_doStaticRotation = value; }
    }
    private OMV.Vector3 m_staticRotationAxis = OMV.Vector3.Zero;
    public OMV.Vector3 StaticRotationAxis {
        get { return m_staticRotationAxis; }
        set { m_staticRotationAxis = value; }
    }
    private float m_staticRotationRotPerSec = 0f;
    public float StaticRotationRotPerSec {
        get { return m_staticRotationRotPerSec; }
        set { m_staticRotationRotPerSec = value; }
    }


}
}
