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
using KeeKee.Framework.Logging;
using KeeKee.Rest;
using KeeKee.World;

using OMV = OpenMetaverse;
using OMVSD = OpenMetaverse.StructuredData;

namespace KeeKee.World.Services {

    /// <summary>
    /// Service (loaded as a module) that listens for avatars coming and going
    /// from the world and presenting a web interface of avatar presence and
    /// statistics.
    /// </summary>
    public class AvatarTracker : IAvatarTrackerService {

        private KLogger<AvatarTracker> m_log;
        protected IWorld m_world;
        protected RestHandlerFactory m_restFactory;
        protected IRestHandler m_restHandler;


        public AvatarTracker(KLogger<AvatarTracker> pLog,
                        RestHandlerFactory pRestFactory,
                        IWorld pWorld
                    ) {
            m_log = pLog;
            m_restFactory = pRestFactory;
            m_world = pWorld;

            m_log.Log(KLogLevel.DINIT, "AvatarTracker.Init()");

            m_restHandler = m_restFactory.CreateHandler<RestHandlerAvatarTracker>();

        }

        public void Dispose() {
            m_log.Log(KLogLevel.DINIT, "AvatarTracker.Dispose()");
            m_restHandler?.Dispose();
        }

    }
}
