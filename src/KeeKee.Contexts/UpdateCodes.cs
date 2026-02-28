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

using KeeKee.Framework;

namespace KeeKee.Contexts {

    public enum UpdateCodes : uint {
        None = 0,
        AttachmentPoint = 1 << 0,
        Material = 1 << 1,
        ClickAction = 1 << 2,
        Scale = 1 << 3,
        ParentID = 1 << 4,
        PrimFlags = 1 << 5,
        PrimData = 1 << 6,
        MediaURL = 1 << 7,
        ScratchPad = 1 << 8,
        Textures = 1 << 9,
        TextureAnim = 1 << 10,
        NameValue = 1 << 11,
        Position = 1 << 12,
        Rotation = 1 << 13,
        Velocity = 1 << 14,
        Acceleration = 1 << 15,
        AngularVelocity = 1 << 16,
        CollisionPlane = 1 << 17,
        Text = 1 << 18,
        Particles = 1 << 19,
        ExtraData = 1 << 20,
        Sound = 1 << 21,
        Joint = 1 << 22,
        Terrain = 1 << 23,
        Focus = 1 << 24,
        Light = 1 << 25,
        Animation = 1 << 26,
        Appearance = 1 << 27,
        New = 1 << 30,  // a new item
        FullUpdate = 0x0fffffff
    }

    public static class UpdateCodesUtil {
        // convert an UpdateCodes value to a string listing the individual codes
        public static string UpdateCodesToString(UpdateCodes what) {
            if (what == UpdateCodes.None) return "None";
            List<string> parts = new();
            foreach (UpdateCodes code in Enum.GetValues(typeof(UpdateCodes))) {
                if (code != UpdateCodes.None && what.HasFlag(code)) {
                    parts.Add(code.ToString());
                }
            }
            return string.Join(", ", parts);
        }
    }
}

