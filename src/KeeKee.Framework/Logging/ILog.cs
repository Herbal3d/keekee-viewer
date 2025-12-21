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

namespace KeeKee.Framework.Logging {

    public enum LogLevel {
        DINIT         = 0x00000001,
        DINITDETAIL   = 0x40000002,
        DVIEW         = 0x00000004,
        DVIEWDETAIL   = 0x40000008,
        DWORLD        = 0x00000010,
        DWORLDDETAIL  = 0x40000020,
        DCOMM         = 0x00000040,
        DCOMMDETAIL   = 0x40000080,
        DREST         = 0x00000100,
        DRESTDETAIL   = 0x40000200,
        DRENDER       = 0x00000400,
        DRENDERDETAIL = 0x40000800, // 1073743872
        DTEXTURE      = 0x00001000,
        DTEXTUREDETAIL= 0x40002000,
        DMODULE       = 0x00004000,
        DMODULEDETAIL = 0x40008000,
        DUPDATE       = 0x00010000,
        DUPDATEDETAIL = 0x40020000,
        DRADEGAST     = 0x00040000,
        DRADEGASTDETAIL=0x40080000,
        DOGRE         = 0x00100000,
        DOGREDETAIL   = 0x40200000,

        DNONDETAIL    = 0x05555555, // all  the non-detail enables
        DDETAIL       = 0x40000000, // 1073741824
        DALL          = 0x7fffffff, // 2147483647
        DBADERROR     = 0x7fffffff,
}

    public interface ILog {
        bool WouldLog(LogLevel logLevel);
        void Log(LogLevel logLevel, string msg);
        void Log(LogLevel logLevel, string msg, object p1);
        void Log(LogLevel logLevel, string msg, object p1, object p2);
        void Log(LogLevel logLevel, string msg, object p1, object p2, object p3);
        void Log(LogLevel logLevel, string msg, object p1, object p2, object p3, object p4);
    }
}
