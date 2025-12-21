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
using System.IO;
using System.Text;
using OMV = OpenMetaverse;

namespace KeeKee {
    /// <summary>
    /// Every program has a place to put general, useful, tool routines.
    /// </summary>
public class Utilities {
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
        string f = first.EndsWith("/") ? first.Substring(first.Length-1) : first;
        string l = last.StartsWith("/") ? last.Substring(1, last.Length-1) : last;
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
        string f = first.EndsWith(separator) ? first.Substring(first.Length-1) : first;
        string l = last.StartsWith(separator) ? last.Substring(1, last.Length-1) : last;
        return f + separator + l;
    }

    /// <summary>
    /// The stupid application storage function MS defined adds "corporation/application/version"
    /// to the end of the application path. This takes them off and just adds the application name.
    /// </summary>
    /// <returns></returns>
    public static string GetDefaultApplicationStorageDir(string subdir) {
        string appdir = System.Windows.Forms.Application.UserAppDataPath;
        string[] pieces = appdir.Split(Path.DirectorySeparatorChar);
        string newAppDir = pieces[0];
        if (pieces.Length > 3) {
            newAppDir = String.Join(System.Char.ToString(Path.DirectorySeparatorChar), pieces, 0, pieces.Length - 3);
        }
        newAppDir = Path.Combine(newAppDir, KeeKeeBase.ApplicationName);
        if ((subdir != null) && (subdir.Length > 0)) newAppDir = Path.Combine(newAppDir, subdir);
        return newAppDir;
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

}
}
