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
public class LLSpecialRenderType : ISpecialRender {
    private SpecialRenderTypes m_type;
    public SpecialRenderTypes Type { get { return m_type; } set { m_type = value; } }

    private OMV.PCode m_foliageType;
    public OMV.PCode FoliageType { get { return m_foliageType; } set { m_foliageType = value; } }
    private OMV.Tree m_treeType;
    public OMV.Tree TreeType { get { return m_treeType; } set { m_treeType = value; } }
    private OMV.Grass m_grassType;
    public OMV.Grass GrassType { get { return m_grassType; } set { m_grassType = value; } }
}
}
