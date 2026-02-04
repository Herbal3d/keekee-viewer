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

namespace KeeKee.Contexts {

    /// <summary>
    /// EntityName class to hold the name of an entity.
    /// 
    /// The name is to be an URI style name with a host and entity ID.
    /// Names will have components and parts and conversion methods
    /// to convert between forms. This is where they all hide.
    /// The goal is to have this class hold the details of the name
    /// thus allowing recreation of the name.
    /// 
    /// The basic name is available with the toString() method or
    /// the .Name property.
    /// 
    /// At the moment this is a simple wrapper around a string.
    /// More complex parsing and handling will be added as needed.
    /// 
    /// There is a lot in this routine from the old EntityName class that
    /// was built around the LLLP texture naming conventions. This will be
    /// reworked as needed to handle other naming conventions.
    /// THe code here exists mainly to prevent errors in older code
    /// that is in the process of being ported.
    /// 
    /// </summary>
    public class EntityName : IComparable {

        public const string HeaderSeparator = ":";
        public const string PartSeparator = "/";

        public string Name { get; set; }

        public static explicit operator string(EntityName name) {
            return name.Name;
        }

        public IAssetContext? AssetContext { get; }

        public string CacheFilename {
            get {
                return Name;
            }
        }
        public string HeaderPart { get { return ""; } }
        public string HostPart { get { return AssetContext?.Name ?? ""; } }
        public string EntityPart { get { return Name; } }


        public EntityName(string pName) {
            Name = pName;
        }

        public EntityName(IAssetContext pAssetContext, string pName) {
            AssetContext = pAssetContext;
            Name = pName;
        }

        public EntityName(EntityName pOther) {
            Name = pOther.Name;
            AssetContext = pOther.AssetContext;
        }

        // Raw routine for combining the parts of the name.
        // We still don't handle headers properly
        // Can be overridden for context specific changes
        public virtual string CombineEntityName(string header, string host, string ent) {
            string ret = "";
            if (String.IsNullOrEmpty(header) == false) {
                if (header.EndsWith(HeaderSeparator)) {
                    ret = header;
                } else {
                    ret = header + HeaderSeparator;
                }
            }
            if (String.IsNullOrEmpty(host) == false) {
                ret += host + ent;
            } else {
                ret += host + PartSeparator + ent;
            }
            return ret;
        }


        public int CompareTo(object? other) {
            if (other is EntityName) {
                return this.Name.CompareTo(((EntityName)other).Name);
            }
            if (other is String) {
                return this.Name.CompareTo((string)other);
            }
            throw new ArgumentException("EntityName.CompareTo: passed non EntityName or String");
        }
        public override string ToString() {
            return Name;
        }

        // we really don't do headers yet
        // Can be overridden for context specific changes
        public virtual string ExtractHeaderPartFromEntityName() {
            return "";
        }

        // default way to get the host part out of an entity name
        // The default format is HOSTPART + "/" + ENTITYPART
        // Can be overridden for context specific changes
        public virtual string ExtractHostPartFromEntityName() {
            int pos1 = this.Name.IndexOf(HeaderSeparator);
            int pos = this.Name.IndexOf(PartSeparator);
            if (pos > 0) {
                return this.Name.Substring(0, pos);
            }
            return "";
        }

        // Can be overridden for context specific changes
        // we assume it's whatever's after the first slash
        public virtual string ExtractEntityFromEntityName() {
            int pos = this.Name.IndexOf(PartSeparator);
            if (pos >= 0) {
                return this.Name.Substring(pos + 1);
            }
            return "";
        }
    }
}
