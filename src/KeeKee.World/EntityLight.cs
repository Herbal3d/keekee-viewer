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
using OpenMetaverse;

namespace KeeKee.World {
    /// <summary>
    /// Lights that fill the world. Used for sun and moon. Individual object 
    /// lighting is done by the entities themselves.
    /// </summary>
    public class EntityLight : EntityBase {
        protected bool m_visible = false;
        virtual public bool Visible { get { return m_visible; } set { m_visible = value; } }

        protected Color4 m_color;
        virtual public Color4 Color { get { return m_color; } set { m_color = value; } }

        protected Vector3 m_position;
        virtual public Vector3 Position { get { return m_position; } set { m_position = value; } }

        protected Vector3 m_target;
        virtual public Vector3 Target { get { return m_target; } set { m_target = value; } }

        public EntityLight(RegionContextBase rcontext, AssetContextBase acontext) 
                    : base(rcontext, acontext) {
        }

        public override void Dispose() {
            throw new NotImplementedException();
        }

    }
}
