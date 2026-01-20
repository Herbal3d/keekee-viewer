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

using System.Reflection;

using OMV = OpenMetaverse;
using OMVSD = OpenMetaverse.StructuredData;

namespace KeeKee.Framework.Utilities {

    /// <summary>
    /// Every program has a place to put general, useful, tool routines.
    /// </summary>
    public static class Utilities {
        /// <summary>
        /// Combine two strings into one longer url. We made sure there is only one
        /// slash between the two joined halves. This means we check for and remove
        /// any extra slash at the end of the first string or the beginning of the last
        /// string.
        /// </summary>
        /// <param name="first"></param>
        /// <param name="last"></param>
        /// <returns></returns>
        public static string JoinURLPieces(string first, string last) {
            string f = first.EndsWith("/") ? first.Substring(first.Length - 1) : first;
            string l = last.StartsWith("/") ? last.Substring(1, last.Length - 1) : last;
            return f + "/" + l;
        }

        /// <summary>
        /// Combine two filename pieces so there is one directory separator between.
        /// This replaces System.IO.Path.Combine which has the nasty feature that it
        /// ignores the first string if the second begins with a separator.
        /// It assumes that it's root and you don't want to join. Wish they asked
        /// me.
        /// </summary>
        /// <param name="first"></param>
        /// <param name="last"></param>
        /// <returns></returns>
        public static string JoinFilePieces(string first, string last) {
            // string separator = "" + Path.DirectorySeparatorChar;
            string separator = "/";     // .NET and mono just use the forward slash
            string f = first.EndsWith(separator) ? first.Substring(0, first.Length - 1) : first;
            string l = last.StartsWith(separator) ? last.Substring(1) : last;
            return f + separator + l;
        }

        public static int TickCountMask = 0x3fffffff;
        public static int TickCount() {
            return System.Environment.TickCount & TickCountMask;
        }
        public static int TickCountSubtract(int prev) {
            int ret = TickCount() - prev;
            if (ret < 0) ret += TickCountMask + 1;
            return ret;
        }

        // Rotate a vector by a quaternian
        public static OMV.Vector3 RotateVector(OMV.Quaternion q, OMV.Vector3 v) {
            OMV.Vector3 v2, v3;
            q.Normalize();
            OMV.Vector3 qv = new OMV.Vector3(q.X, q.Y, q.Z);
            v2 = OMV.Vector3.Cross(qv, v);
            v3 = OMV.Vector3.Cross(qv, v2);
            v2 *= (2.0f * q.W);
            v3 *= 2.0f;
            return v + v2 + v3;
        }

        public static string GetMimeTypeFromFileName(string fileName) {
            string ext = Path.GetExtension(fileName).ToLower();
            switch (ext) {
                case ".htm":
                case ".html":
                    return "text/html";
                case ".css":
                    return "text/css";
                case ".js":
                    return "application/javascript";
                case ".png":
                    return "image/png";
                case ".jpg":
                case ".jpeg":
                    return "image/jpeg";
                case ".gif":
                    return "image/gif";
                case ".svg":
                    return "image/svg+xml";
                case ".ico":
                    return "image/x-icon";
                case ".json":
                    return "application/json";
                case ".xml":
                    return "application/xml";
                default:
                    return "application/octet-stream";
            }
        }

        /// <summary>
        /// Convert an OSDMap to a class of type T by matching field and property names.
        /// Loops through all the fields in the class and, if a value of the same name
        /// exists in the OSDMap, sets the field to that value (converted to the appropriate type).
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="map"></param>
        /// <returns></returns>
        public static T OSDMapToClass<T>(OMVSD.OSDMap map) where T : new() {
            T obj = new T();
            var objType = typeof(T);

            // Get all fields
            var fields = objType.GetFields(BindingFlags.Public);
            var properties = objType.GetProperties(BindingFlags.Public);
            foreach (var fieldInfo in fields) {
                if (map.ContainsKey(fieldInfo.Name)) {
                    fieldInfo.SetValue(obj, Convert.ChangeType(map[fieldInfo.Name], fieldInfo.FieldType));
                }
            }
            foreach (var propertyInfo in properties) {
                if (map.ContainsKey(propertyInfo.Name) && propertyInfo.CanWrite) {
                    propertyInfo.SetValue(obj, Convert.ChangeType(map[propertyInfo.Name], propertyInfo.PropertyType));
                }
            }

            return obj;
        }
    }
}
