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

using KeeKee;
using KeeKee.Framework.Logging;

using OMV = OpenMetaverse;
using OMVSD = OpenMetaverse.StructuredData;

namespace KeeKee.Comm {
    /// <summary>
    /// Someday abstract out the basic object conversion logic (creation of
    /// agents, ...) into this base routine and let the comm specific stuff
    /// be in a subclass of this base.
    /// This class would have no comm (LLLP) or virtual world (LL) specific
    /// code in it. This classes sole purpose is to provide the bridge
    /// classes between comm and KeeKee.World.
    /// for
    /// </summary>
    public class CommBase /*: ICommProvider*/ {
        /*
        string Name { get; }

        bool IsConnected { get; }

        bool IsLoggedIn { get; }

        bool Connect(ParameterSet parms);

        bool Disconnect();

        // initiate a connection
        ParameterSet ConnectionParams { get; }

        // kludge to get underlying LL Comm (circular ref Comm.LLLP <=> World.LL)
        OMV.GridClient GridClient { get; }

        // each comm provider has a block of statistics
        ParameterSet CommStatistics();
         */
    }
}
