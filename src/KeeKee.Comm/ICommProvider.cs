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

using KeeKee;
using KeeKee.Framework;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

using OMV = OpenMetaverse;

namespace KeeKee.Comm {

    public delegate void ConnectionFailureCallback(ICommProvider source, string reason);

    public interface ICommProvider : IProvider {
        string Name { get; }

        bool IsConnected { get; }

        bool IsLoggedIn { get; }

        bool Connect(IOptions<CommConfig> parms);

        bool Disconnect();

        // initiate a connection
        IOptions<CommConfig> ConnectionParams { get; }

        // kludge to get underlying LL Comm (circular ref Comm.LLLP <=> World.LL)
        OMV.GridClient GridClient { get; }

        // each comm provider has a block of statistics
        CommStats CommStatistics();
    }
}
