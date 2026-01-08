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

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using KeeKee.Framework;
using KeeKee.Config;
using KeeKee.Framework.Logging;
using KeeKee.Framework.WorkQueue;
using KeeKee.Framework.Statistics;
using KeeKee.World;
using KeeKee.World.LL;

using OMV = OpenMetaverse;

namespace KeeKee.Comm.LLLP {
    /// <summary>
    /// Communication handler for Linden Lab Legacy Protocol
    /// </summary>
    public class CommLLLP : BackgroundService, ICommProvider {
        private KLogger<CommLLLP> m_log;

        public IOptions<CommConfig> ConnectionConfig { get; set; }
        public IOptions<AssetConfig> AssetsConfig { get; set; }
        public IOptions<LLAgentConfig> LLAgentConfig { get; set; }

        public IAssetContext GridsAssetContext { get; private set; }

        private CancellationToken m_cancellationToken;

        // ICommProvider.Name
        public string Name { get { return "CommLLLP"; } }


        // Statistics ===============================================
        // ICommProvider.CommStatistics
        public StatisticCollection CommStatistics { get; private set; }
        private Stat<long> m_statNetDisconnected = new StatCounter("Network_Disconnected", "Number of 'network disconnected' messages");
        private Stat<long> m_statNetLoginProgress = new StatCounter("Network_LoginProgress", "Number of 'login progress' messages");
        private Stat<long> m_statNetSimChanged = new StatCounter("Network_SimChanged", "Number of 'sim changed' messages");
        private Stat<long> m_statNetSimConnected = new StatCounter("Network_SimConnected", "Number of 'sim connected' messages");
        private Stat<long> m_statNetEventQueueRunning = new StatCounter("Network_EventQueueRunning", "Number of 'event queue running' messages");
        private Stat<long> m_statObjAttachmentUpdate = new StatCounter("Object_AttachmentUpdate", "Number of 'attachment update' messages");
        private Stat<long> m_statObjAvatarUpdate = new StatCounter("Object_AvatarUpdate", "Number of 'avatar update' messages");
        private Stat<long> m_statObjKillObject = new StatCounter("Object_KillObject", "Number of 'kill object' messages");
        private Stat<long> m_statObjObjectProperties = new StatCounter("Object_ObjectProperties", "Number of 'object properties' messages");
        private Stat<long> m_statObjObjectPropertiesUpdate = new StatCounter("Object_ObjectPropertiesUpdate", "Number of 'object properties update' messages");
        private Stat<long> m_statObjObjectUpdate = new StatCounter("Object_ObjectUpdate", "Number of 'object update' messages");
        private Stat<long> m_statObjTerseUpdate = new StatCounter("Object_TerseObjectUpdate", "Number of 'terse object update' messages");
        private Stat<long> m_statRequestLocalID = new StatCounter("RequestLocalID", "Number of RequestLocalIDs made");
        // ==========================================================

        public ILLInstanceFactory InstanceFactory { get; private set; }

        private IWorld m_World;
        public Grids GridList;

        // ICommProvider.GridClient
        public OMV.GridClient GridClient { get; private set; }

        // list of the region information build for the simulator
        protected Dictionary<OMV.UUID, LLRegionContext> m_regionList = new Dictionary<OMV.UUID, LLRegionContext>();

        // while we wait for a region to be online, we queue requests here
        protected List<ParamBlock> m_waitTilOnline = new List<ParamBlock>();
        protected BasicWorkQueue m_waitTilLater = new BasicWorkQueue("CommDoTilLater");

        // There are some messages that come in that are rare but could use some locking.
        // The main paths of prims and updates is pretty solid and multi-threaded but
        // others, like avatar control, can use a little locking.
        private Object m_opLock = new Object();

        /// <summary>
        /// Flag saying we're switching simulator connections. This would suppress things like teleport
        /// and certain status indications.
        /// </summary>
        public bool SwitchingSims { get { return m_SwitchingSims; } }
        protected bool m_SwitchingSims;       // true when we're setting up the connection to a different sim

        // The whole module is loaded or unloaded. This controls the whole trying to login loop.
        // m_shouldBeLoggedIn says whether we think we should be logged in. If true then the
        // first, last, ... parameters have the info to use logging in.
        // The logging in and out flags are true when we're doing that. Use to make sure
        // we don't try logging in or out again.
        // The module flag 'm_connected' is set true when logged in and connected.
        protected bool m_loaded { get; set; } = false;  // if comm is loaded and should be trying to connect
        protected bool m_shouldBeLoggedIn { get; set; } = false; // true if we should be logged in
        protected LoginParams? m_loginParams { get; set; } // parameters to use when logging in
        protected bool m_isLoggingIn { get; set; } = false;  // true if we are in the process of loggin in
        protected bool m_isLoggingOut { get; set; } = false; // true if we are in the process of logging out

        // m_loginGrid has the displayable name. LoggedInGridName has cannoicalized name for app use.
        protected string m_loginGrid { get; set; } = "unknown";
        protected string LoggedInGridName { get { return m_loginGrid.Replace(".", "_").ToLower(); } }
        protected string m_loginMsg { get; set; } = "";

        // If true, hold children objects until parent is available
        protected bool m_shouldHoldChildren = false;

        protected UserPersistantParams m_userPersistantParams;


        // There is one entity who is the main agent we control
        protected LLEntity? MainAgent { get; set; } = null;

        public CommLLLP(KLogger<CommLLLP> pLog,
                        UserPersistantParams pUserParams,
                        IOptions<CommConfig> pConnectionConfig,
                        IOptions<AssetConfig> pAssetsConfig,
                        IOptions<LLAgentConfig> pLLAgentConfig,
                        Grids pGrids,
                        IAssetContext pAssetContext,
                        ILLInstanceFactory pInstanceFactory,
                        IWorld pWorld) {
            m_log = pLog;
            m_userPersistantParams = pUserParams;
            ConnectionConfig = pConnectionConfig;
            AssetsConfig = pAssetsConfig;
            LLAgentConfig = pLLAgentConfig;
            GridsAssetContext = pAssetContext;
            GridList = pGrids;
            InstanceFactory = pInstanceFactory;
            m_World = pWorld;

            CommStatistics = new StatisticCollection();
            CommStatistics.AddStat(m_statNetDisconnected);
            CommStatistics.AddStat(m_statNetLoginProgress);
            CommStatistics.AddStat(m_statNetSimChanged);
            CommStatistics.AddStat(m_statNetSimConnected);
            CommStatistics.AddStat(m_statNetEventQueueRunning);
            CommStatistics.AddStat(m_statObjAttachmentUpdate);
            CommStatistics.AddStat(m_statObjAvatarUpdate);
            CommStatistics.AddStat(m_statObjKillObject);
            CommStatistics.AddStat(m_statObjObjectProperties);
            CommStatistics.AddStat(m_statObjObjectPropertiesUpdate);
            CommStatistics.AddStat(m_statObjObjectUpdate);
            CommStatistics.AddStat(m_statObjTerseUpdate);
            CommStatistics.AddStat(m_statRequestLocalID);

            GridClient = new OMV.GridClient();
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken) {
            m_log.Log(KLogLevel.RestDetail, "CommLLLP ExecuteAsync entered");
            m_cancellationToken = cancellationToken;

            while (!cancellationToken.IsCancellationRequested) {
                InitConnectionFramework();

                while (m_loaded && !cancellationToken.IsCancellationRequested) {
                    if (m_shouldBeLoggedIn && !IsLoggedIn) {
                        // we should be logged in and we are not
                        if (!m_isLoggingIn) {
                            await StartLogin();
                        }
                    }
                    if (!cancellationToken.IsCancellationRequested && !IsLoggedIn && IsConnected) {
                        // if we're not supposed to be running, disconnect everything
                        m_log.Log(KLogLevel.DCOMM, "KeepLoggedIn: Shutting down the network");
                        GridClient.Network.Shutdown(OpenMetaverse.NetworkManager.DisconnectType.ClientInitiated);
                        IsConnected = false;
                    }
                    if (!cancellationToken.IsCancellationRequested || (!m_shouldBeLoggedIn && IsLoggedIn)) {
                        // we shouldn't be logged in but it looks like we are
                        m_log.Log(KLogLevel.DCOMM, "KeepLoggedIn: Shouldn't be logged in");
                        if (!m_isLoggingIn && !m_isLoggingOut) {
                            // not in logging transistion. start the logout process
                            m_log.Log(KLogLevel.DCOMM, "KeepLoggedIn: Starting logout process");
                            GridClient.Network.Logout();
                            m_isLoggingIn = false;
                            m_isLoggingOut = true;
                            IsLoggedIn = false;
                            m_shouldBeLoggedIn = false;
                        }
                    }
                    // TODO: update our login parameters for the UI

                    await Task.Delay(500, cancellationToken);
                }
                DisconnectConnectionFramework();

                m_log.Log(KLogLevel.DCOMM, "KeepLoggingIn: exiting keep loggin in thread");
            }
        }

        /* OLD CODE THAT PROBABLY CAN BE DELETED
        /// <summary>
        /// The statistics ParameterSet has some delegated values that are only valid
        /// when logging in, etc. When the values are asked for, this routine is called
        /// delegate which calculates the current values of the statistic.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        protected OMVSD.OSD RuntimeValueFetch(string key) {
            OMVSD.OSD ret = null;
            try {
                if ((GridClient != null) && (IsConnected && m_isLoggedIn)) {
                    switch (key) {
                        case FIELDCURRENTSIM:
                            ret = new OMVSD.OSDString(GridClient.Network.CurrentSim.Name);
                            break;
                        case FIELDCURRENTGRID:
                            ret = new OMVSD.OSDString(m_loginGrid);
                            break;
                        case FIELDPOSITIONX:
                            ret = new OMVSD.OSDString(GridClient.Self.SimPosition.X.ToString());
                            break;
                        case FIELDPOSITIONY:
                            ret = new OMVSD.OSDString(GridClient.Self.SimPosition.Y.ToString());
                            break;
                        case FIELDPOSITIONZ:
                            ret = new OMVSD.OSDString(GridClient.Self.SimPosition.Z.ToString());
                            break;
                    }
                    if (ret != null) return ret;
                }
            } catch (Exception e) {
                m_log.Log(KLogLevel.DCOMM, "RuntimeValueFetch: failure getting {0}: {1}", key, e.ToString());
            }
            return new OMVSD.OSDString("");
        }
        */

        protected void InitConnectionFramework() {
            // Initialize the SL client
            try {
                var gc = GridClient;
                // GridClient.Settings.ENABLE_CAPS = true;
                gc.Settings.ENABLE_SIMSTATS = true;
                gc.Settings.MULTIPLE_SIMS = ConnectionConfig.Value.MultipleSims;
                gc.Settings.ALWAYS_DECODE_OBJECTS = true;
                gc.Settings.ALWAYS_REQUEST_OBJECTS = true;
                gc.Settings.OBJECT_TRACKING = true; // We use our own object tracking system
                gc.Settings.AVATAR_TRACKING = true; //but we want to use the libsl avatar system
                gc.Settings.SEND_AGENT_APPEARANCE = true;    // for the moment, don't do appearance
                gc.Settings.SEND_AGENT_THROTTLE = true;    // tell them how fast we want it when connected
                gc.Settings.PARCEL_TRACKING = true;
                gc.Settings.ALWAYS_REQUEST_PARCEL_ACL = false;
                gc.Settings.ALWAYS_REQUEST_PARCEL_DWELL = false;
                gc.Settings.USE_INTERPOLATION_TIMER = false;  // don't need the library helping
                gc.Settings.SEND_AGENT_UPDATES = true;
                gc.Self.Movement.AutoResetControls = false;
                gc.Self.Movement.UpdateInterval = ConnectionConfig.Value.MovementUpdateInterval;
                gc.Settings.DISABLE_AGENT_UPDATE_DUPLICATE_CHECK = false;
                gc.Settings.USE_ASSET_CACHE = false;
                gc.Settings.PIPELINE_REQUEST_TIMEOUT = 120 * 1000;
                gc.Settings.ASSET_CACHE_DIR = AssetsConfig.Value.CacheDir;
                OMV.Settings.RESOURCE_DIR = AssetsConfig.Value.OMVResources;
                // Crank up the throttle on texture downloads
                gc.Throttle.Total = 20000000.0f;
                gc.Throttle.Texture = 2446000.0f;
                gc.Throttle.Asset = 2446000.0f;
                gc.Settings.THROTTLE_OUTGOING_PACKETS = false;

                // gc.Network.LoginProgress += Network_LoginProgress;
                gc.Network.Disconnected += Network_Disconnected;
                gc.Network.SimConnected += Network_SimConnected;
                gc.Network.EventQueueRunning += Network_EventQueueRunning;
                gc.Network.SimChanged += Network_SimChanged;
                gc.Network.EventQueueRunning += Network_EventQueueRunning;

                gc.Objects.ObjectPropertiesUpdated += Objects_ObjectPropertiesUpdated;
                gc.Objects.ObjectUpdate += Objects_ObjectUpdate;
                gc.Objects.ObjectDataBlockUpdate += Objects_ObjectDataBlockUpdate;
                gc.Objects.ObjectProperties += Objects_ObjectProperties;
                gc.Objects.TerseObjectUpdate += Objects_TerseObjectUpdate;
                gc.Objects.AvatarUpdate += Objects_AvatarUpdate;
                gc.Objects.KillObject += Objects_KillObject;
                gc.Avatars.AvatarAppearance += Avatars_AvatarAppearance;
                gc.Settings.STORE_LAND_PATCHES = true;
                gc.Terrain.LandPatchReceived += Terrain_LandPatchReceived;

            } catch (Exception e) {
                m_log.Log(KLogLevel.DBADERROR, "EXCEPTION BUILDING GRIDCLIENT: " + e.ToString());
            }

            // fake like this is the initial teleport
            m_SwitchingSims = true;
        }
        private void DisconnectConnectionFramework() {
            var gc = GridClient;
            // gc.Network.LoginProgress -= Network_LoginProgress;
            gc.Network.Disconnected -= Network_Disconnected;
            gc.Network.SimConnected -= Network_SimConnected;
            gc.Network.EventQueueRunning -= Network_EventQueueRunning;
            gc.Network.SimChanged -= Network_SimChanged;
            gc.Network.EventQueueRunning -= Network_EventQueueRunning;

            gc.Objects.ObjectPropertiesUpdated -= Objects_ObjectPropertiesUpdated;
            gc.Objects.ObjectUpdate -= Objects_ObjectUpdate;
            gc.Objects.ObjectDataBlockUpdate -= Objects_ObjectDataBlockUpdate;
            gc.Objects.ObjectProperties -= Objects_ObjectProperties;
            gc.Objects.TerseObjectUpdate -= Objects_TerseObjectUpdate;
            gc.Objects.AvatarUpdate -= Objects_AvatarUpdate;
            gc.Objects.KillObject -= Objects_KillObject;
            gc.Avatars.AvatarAppearance -= Avatars_AvatarAppearance;
            gc.Terrain.LandPatchReceived -= Terrain_LandPatchReceived;
        }

        // ICommProvider.DoLogin()
        /// <summary>
        /// Called by the REST handler to connect to a simulator.
        /// The login parameters are passed in which is the autorization info.
        /// Sets the state to "should be logged in" and processing should continue.
        /// </summary>
        /// <param name="pLoginParams"></param>
        /// <returns></returns>
        public async Task<OMV.LoginResponseData?> DoLogin(LoginParams pLoginParams) {
            // Are we already logged in?
            if (IsLoggedIn || m_isLoggingIn) {
                return null;
            }

            m_loginParams = pLoginParams;
            m_shouldBeLoggedIn = true;

            var loginResponse = await StartLogin();

            return loginResponse;
        }

        // ICommProvider.StartLogout()
        public virtual bool StartLogout() {
            m_log.Log(KLogLevel.DCOMMDETAIL, "Disconnect request -- logout and disconnect");
            m_shouldBeLoggedIn = false;
            return true;
        }

        // ICommProvider.StartTeleport()
        public virtual bool StartTeleport(string dest) {
            bool ret = true;
            string sim = "";
            float x = 128;
            float y = 128;
            float z = 40;
            dest = dest.Trim();
            string[] tokens = dest.Split(new char[] { '/' });
            if (tokens.Length == 4) {
                sim = tokens[0];
                if (!float.TryParse(tokens[1], out x) ||
                                !float.TryParse(tokens[2], out y) ||
                                !float.TryParse(tokens[3], out z)) {
                    m_log.Log(KLogLevel.DBADERROR, "Could not parse teleport destination '{0}'", dest);
                    ret = false;
                }
            } else if (tokens.Length == 1) {
                sim = tokens[0];
                x = 128;
                y = 128;
                z = 40;
            } else {
                m_log.Log(KLogLevel.DBADERROR, "Did not recognize format of teleport destination: '{0}'", dest);
                ret = false;
            }
            if (ret && IsLoggedIn && (GridClient != null)) {
                if (GridClient.Self.Teleport(sim, new OMV.Vector3(x, y, z))) {
                    m_log.Log(KLogLevel.DBADERROR, "Teleport successful to '{0}'", dest);
                    ret = true;
                } else {
                    m_log.Log(KLogLevel.DBADERROR, "Teleport to '{0}' failed", dest);
                    ret = false;
                }
            }
            return ret;
        }

        // ICommProvider.StartLogin()
        public async Task<OMV.LoginResponseData?> StartLogin() {
            if (m_loginParams == null) {
                m_log.Log(KLogLevel.DBADERROR, "StartLogin: no login parameters");
                return null;
            }
            m_log.Log(KLogLevel.DCOMM, "Starting login of {0} {1}", m_loginParams.FirstName, m_loginParams.LastName);
            m_isLoggingIn = true;
            OMV.LoginParams loginParams = GridClient.Network.DefaultLoginParams(
                m_loginParams.FirstName,
                m_loginParams.LastName,
                m_loginParams.Password,
                ConnectionConfig.Value.ApplicationName,
                ConnectionConfig.Value.Version
            );

            // Select sim in the grid
            // the format that we must pass is "uri:sim&x&y&z" or the strings "home" or "last"
            // The user inputs either "home", "last", "sim" or "sim/x/y/z"
            string loginSetting = "";
            string startLoc = m_loginParams.StartLocation ?? "";
            if (!String.IsNullOrEmpty(startLoc)) {
                try {
                    // User specified a sim. In the form of "simname/x/y/z" where the coords are optional.
                    char sep = '/';
                    string[] parts = System.Uri.UnescapeDataString(startLoc).ToLower().Split(sep);
                    if (parts.Length > 0) {
                        // since the name comes in through the web page, spaces get turned into pluses
                        parts[0] = parts[0].Replace('+', ' ');
                    }
                    loginSetting = parts[0];    // default to just the sim name
                    if (parts.Length == 1) {
                        // just specifying last or home or just a simulator
                        if (parts[0] == "last" || parts[0] == "home") {
                            m_log.Log(KLogLevel.DCOMM, "StartLogin: prev location of {0}", parts[0]);
                            loginSetting = parts[0];
                        } else {
                            // put the user in the center of the specified sim
                            loginSetting = OMV.NetworkManager.StartLocation(parts[0], 128, 128, 40);
                            m_log.Log(KLogLevel.DCOMM, "StartLogin: user spec middle of {0} -> {1}", parts[0], loginSetting);
                        }
                    }
                    if (parts.Length == 4) {
                        int posX = int.Parse(parts[1]);
                        int posY = int.Parse(parts[2]);
                        int posZ = int.Parse(parts[3]);
                        loginSetting = OMV.NetworkManager.StartLocation(parts[0], posX, posY, posZ);
                        m_log.Log(KLogLevel.DCOMM, "StartLogin: user spec start at {0}/{1}/{2}/Z -> {3}",
                            parts[0], posX, posY, loginSetting);
                    }
                } catch {
                    loginSetting = "";
                }
            }
            // if we didn't get anything useful, default to last
            loginParams.Start = String.IsNullOrEmpty(loginSetting) ? "last" : loginSetting;

            GridList.SetCurrentGrid(m_loginGrid);
            loginParams.URI = GridList.GridLoginURI(GridList.CurrentGrid);
            if (loginParams.URI == null) {
                m_log.Log(KLogLevel.DBADERROR, "COULD NOT FIND URL OF GRID. Grid=" + m_loginGrid);
                m_loginMsg = "Unknown Grid name";
                m_isLoggingIn = false;
                m_shouldBeLoggedIn = false;
            } else {
                try {
                    OMV.LoginResponseData response = await GridClient.Network.LoginWithResponseAsync(loginParams, m_cancellationToken);
                    if (response.Success) {
                        m_log.Log(KLogLevel.DCOMM, "Login successful: {0}", response.Message);
                        // m_isConnected = true;
                        IsLoggedIn = true;
                        m_isLoggingIn = false;
                        m_loginMsg = response.Message;
                        Comm_OnLoggedIn();
                    } else {
                        m_log.Log(KLogLevel.DCOMM, "Login failed: {0}", response.Message);
                        m_isLoggingIn = false;
                        m_shouldBeLoggedIn = false;
                        m_loginMsg = response.Message;
                    }
                    return response;
                } catch (Exception e) {
                    m_log.Log(KLogLevel.DBADERROR, "BeginLogin exception: " + e.ToString());
                    m_isLoggingIn = false;
                    m_shouldBeLoggedIn = false;
                }
            }
            return null;
        }

        public virtual void Network_Disconnected(object? sender, OMV.DisconnectedEventArgs args) {
            this.m_statNetDisconnected.Event();
            m_log.Log(KLogLevel.DCOMM, "Disconnected");
            IsConnected = false;
        }

        /*
        public virtual void Network_EventQueueRunning(object? sender, OMV.EventQueueRunningEventArgs args) {
               this.m_statNetDisconnected.Event();
            m_log.Log(KLogLevel.DCOMM, "Event queue running on {0}", args.Simulator.Name);
            if (args.Simulator == GridClient.Network.CurrentSim) {
                m_SwitchingSims = false;
            }
            // Now seems like a good time to start requesting parcel information
            GridClient.Parcels.RequestAllSimParcels(GridClient.Network.CurrentSim, false, 100);
        }
        */

        public bool IsConnected { get; private set; } = false;

        public bool IsLoggedIn { get; private set; } = false;

        // ===============================================================
        public virtual void Network_SimConnected(object? sender, OMV.SimConnectedEventArgs args) {
            this.m_statNetSimConnected.Event();
            m_log.Log(KLogLevel.DWORLD, "Network_SimConnected: Simulator connected {0}", args.Simulator.Name);
        }

        // ===============================================================
        public virtual void Network_EventQueueRunning(Object? sender, OMV.EventQueueRunningEventArgs args) {
            LLRegionContext regionContext;
            lock (m_opLock) {
                // the sim isn't really up until the caps queue is running
                IsConnected = true;   // good enough reason to think we're connected
                this.m_statNetEventQueueRunning.Event();
                m_log.Log(KLogLevel.DWORLD, "Network_EventQueueRunning: Simulator connected {0}", args.Simulator.Name);

                regionContext = FindRegion(args.Simulator);
                if (regionContext == null) {
                    m_log.Log(KLogLevel.DWORLD, "Network_EventQueueRunning: NO REGION CONTEXT FOR {0}", args.Simulator.Name);
                    return;
                }

                if (regionContext.State.State == RegionStateCode.Online) {
                    m_log.Log(KLogLevel.DWORLD, "Network_EventQueueRunning: Region already online: {0}", args.Simulator.Name);
                    return;
                }
                // a kludge to handle race conditions. We lock the region state while we empty queues
                regionContext.State.State = RegionStateCode.Online;
            }

            // tell the world there is a new region
            m_World.AddRegion(regionContext);

            // regionContext.State.IfOnline(delegate() {
            // this region is online and here. This can start a lot of IO

            // if we'd queued up actions, do them now that it's online
            DoAnyWaitingEvents(args.Simulator);
            // });

            // this is needed to make the avatar appear
            // TODO: figure out if the linking between agent and appearance is right
            // GridClient.Appearance.SetPreviousAppearance(true);
            GridClient.Appearance.RequestSetAppearance(true);
            GridClient.Self.Movement.UpdateFromHeading(0.0, true);
        }

        // ===============================================================
        public virtual void Network_SimChanged(object? sender, OMV.SimChangedEventArgs args) {
            // disable teleports until we have a good connection to the simulator (event queue working)
            this.m_statNetSimChanged.Event();
            if (!GridClient.Network.CurrentSim.Caps.IsEventQueueRunning) {
                m_SwitchingSims = true;
            }
            if (args.PreviousSimulator != null) {      // there is no prev sim the first time
                m_log.Log(KLogLevel.DWORLD, "Simulator changed from {0}", args.PreviousSimulator.Name);
                LLRegionContext regionContext = FindRegion(args.PreviousSimulator);
                if (regionContext == null) return;
                // TODO: what to do with this operation?
            }
        }

        // ===============================================================
        public virtual void Terrain_LandPatchReceived(object? sender, OMV.LandPatchReceivedEventArgs args) {
            // m_log.Log(KLogLevel.DWORLDDETAIL, "Land patch for {0}: {1}, {2}, {3}", 
            //             args.Simulator.Name, args.X, args.Y, args.PatchSize);
            LLRegionContext regionContext = FindRegion(args.Simulator);
            if (regionContext == null) return;
            // update the region's view of the terrain
            regionContext.TerrainInfo.UpdatePatch(regionContext, args.X, args.Y, args.HeightMap);
            // tell the world the earth is moving
            if (QueueTilOnline(args.Simulator, CommActionCode.RegionStateChange, regionContext, World.UpdateCodes.Terrain)) {
                return;
            }
            regionContext.Update(World.UpdateCodes.Terrain);
        }

        // ===============================================================
        public void Objects_ObjectDataBlockUpdate(object? sender, OMV.ObjectDataBlockUpdateEventArgs args) {
            return;
        }

        // ===============================================================
        public void Objects_ObjectUpdate(object? sender, OMV.PrimEventArgs args) {
            if (args.IsAttachment) {
                Objects_AttachmentUpdate(sender, args);
                return;
            }
            if (QueueTilOnline(args.Simulator, CommActionCode.OnObjectUpdated, sender, args)) return;
            lock (m_opLock) {
                LLRegionContext rcontext = FindRegion(args.Simulator);
                if (!ParentExists(rcontext, args.Prim.ParentID)) {
                    // if this requires a parent and the parent isn't here yet, queue this operation til later
                    rcontext.RequestLocalID(args.Prim.ParentID);
                    m_statRequestLocalID.Event();
                    QueueTilLater(args.Simulator, CommActionCode.OnObjectUpdated, sender, args);
                    return;
                }
                m_statObjObjectUpdate.Event();
                IEntity? updatedEntity;
                // a full update says everything changed
                UpdateCodes updateFlags = 0;
                updateFlags |= UpdateCodes.Position | UpdateCodes.Rotation;
                m_log.Log(KLogLevel.DUPDATEDETAIL, "Object update: id={0}, p={1}, r={2}",
                    args.Prim.LocalID, args.Prim.Position.ToString(), args.Prim.Rotation.ToString());
                try {
                    if (rcontext.TryGetCreateEntityLocalID(args.Prim.LocalID, out updatedEntity, delegate () {
                        // code called to create the entry if it's not found
                        updateFlags |= UpdateCodes.New;
                        updateFlags |= UpdateCodes.Acceleration | UpdateCodes.AngularVelocity | UpdateCodes.Velocity;
                        return InstanceFactory.CreateLLPhysical(GridClient, args.Prim);
                    })) {
                        // new prim created
                        // If this requires special rendering parameters add those parameters
                        // At the moment, the only case is foliage
                        if (args.Prim.PrimData.PCode == OpenMetaverse.PCode.Grass
                                    || args.Prim.PrimData.PCode == OpenMetaverse.PCode.Tree
                                    || args.Prim.PrimData.PCode == OpenMetaverse.PCode.NewTree) {
                            LLCmptSpecialRenderType srt = new LLCmptSpecialRenderType(m_log, updatedEntity, GridClient, rcontext);
                            srt.Type = SpecialRenderTypes.Foliage;
                            srt.FoliageType = args.Prim.PrimData.PCode;
                            srt.TreeType = args.Prim.TreeSpecies;
                            updatedEntity.AddComponent<LLCmptSpecialRenderType>(srt);
                        }
                        // if there are animations for this entity
                        ProcessEntityAnimation(updatedEntity, ref updateFlags, args.Prim.AngularVelocity);
                    }
                    // send updates for this entity updates
                    ProcessEntityUpdates(updatedEntity, updateFlags);
                } catch (Exception e) {
                    m_log.Log(KLogLevel.DBADERROR, "FAILED CREATION OF NEW PRIM: " + e.ToString());
                }
            }

            return;
        }

        // return 'true' is the parent of this id exists in the world
        private bool ParentExists(LLRegionContext regionContext, uint parentID) {
            // if shouldn't be holding anything, fake like the parent is always here
            if (!m_shouldHoldChildren) return true;
            // if we don't need a parent no need to check
            if (parentID == 0) return true; // if no parent say we have the parent
                                            // see if the parent is known
            IEntity parentEntity = null;
            regionContext.TryGetEntityLocalID(parentID, out parentEntity);
            return (parentEntity != null);
        }

        // For the moment, create only one animation for an entity and that is the angular rotation.
        private void ProcessEntityAnimation(IEntity ent, ref UpdateCodes updateFlags, OMV.Vector3 angularVelocity) {
            try {
                // if  there is an angular velocity and this is not an avatar, pass the information
                // along as an animation (llTargetOmega)
                // we convert the information into a standard form
                if (angularVelocity != OMV.Vector3.Zero) {
                    float rotPerSec = angularVelocity.Length() / Constants.TWOPI;
                    OMV.Vector3 axis = angularVelocity;
                    axis.Normalize();
                    if (!ent.HasComponent<LLCmptAnimation>()) {
                        var newAnim = new LLCmptAnimation(m_log, ent, GridClient);
                        ent.AddComponent<ICmptAnimation>(newAnim);
                        m_log.Log(KLogLevel.DUPDATEDETAIL, "Created prim animation on {0}", ent.Name);
                    }
                    LLCmptAnimation anim = ent.Cmpt<LLCmptAnimation>();
                    if (rotPerSec != anim.StaticRotationRotPerSec || axis != anim.StaticRotationAxis) {
                        anim.AngularVelocity = angularVelocity;   // legacy. Remove when other part plumbed
                        anim.StaticRotationAxis = axis;
                        anim.StaticRotationRotPerSec = rotPerSec;
                        anim.DoStaticRotation = true;
                        updateFlags |= UpdateCodes.Animation;
                        m_log.Log(KLogLevel.DUPDATEDETAIL, "Updating prim animation on {0}", ent.Name);
                    }
                }
            } catch (Exception e) {
                m_log.Log(KLogLevel.DBADERROR, "FAILED ProcessEntityAnimation: " + e.ToString());
            }
        }

        // Entity has been updated. Tell the world about the updates.
        private void ProcessEntityUpdates(IEntity ent, UpdateCodes updateFlags) {
            try {
                if (ent != null) {
                    // special update for the agent so it knows there is new info from the network
                    // The real logic to push the update through happens in the IEntityAvatar.Update()
                    if (ent == this.MainAgent) {
                        // TODO: figure out if we need to do anything special for the main agent
                        // ent.DataUpdate(updateFlags);
                    }
                    // Tell the world the entity is updated
                    ent.Update(updateFlags);
                }
            } catch (Exception e) {
                m_log.Log(KLogLevel.DBADERROR, "FAILED ProcessEntityUpdates: " + e.ToString());
            }
        }
        // ===============================================================
        // The packet library has updated the attachement points in the prim already
        // This needs to get the attachment loaded into the world
        public void Objects_AttachmentUpdate(object? sender, OMV.PrimEventArgs args) {
            if (QueueTilOnline(args.Simulator, CommActionCode.OnAttachmentUpdate, sender, args)) return;
            lock (m_opLock) {
                LLRegionContext? rcontext = FindRegion(args.Simulator);
                if (rcontext == null) return;

                if (!ParentExists(rcontext, args.Prim.ParentID)) {
                    // if this requires a parent and the parent isn't here yet, queue this operation til later
                    rcontext.RequestLocalID(args.Prim.ParentID);
                    QueueTilLater(args.Simulator, CommActionCode.OnObjectUpdated, sender, args);
                    return;
                }

                m_statObjAttachmentUpdate.Event();
                m_log.Log(KLogLevel.DUPDATEDETAIL, "OnNewAttachment: id={0}, lid={1}", args.Prim.ID.ToString(), args.Prim.LocalID);

                try {
                    // if new or not, assume everything about this entity has changed
                    UpdateCodes updateFlags = UpdateCodes.FullUpdate;
                    IEntity ent;
                    if (rcontext.TryGetCreateEntityLocalID(args.Prim.LocalID, out ent, () => {
                        LLEntity newEnt = InstanceFactory.CreateLLPhysical(GridClient, args.Prim);
                        updateFlags |= UpdateCodes.New;
                        string? attachmentID = "1"; // default attachment ID
                        if (args.Prim.NameValues != null) {
                            foreach (OMV.NameValue nv in args.Prim.NameValues) {
                                m_log.Log(KLogLevel.DCOMMDETAIL, "AttachmentUpdate: ent={0}, {1}->{2}", newEnt.Name, nv.Name, nv.Value);
                                if (nv.Name == "AttachItemID") {
                                    attachmentID = nv.Value.ToString();
                                    break;
                                }
                            }
                        }
                        LLCmptAttachment att = new LLCmptAttachment(m_log, newEnt, GridClient);
                        newEnt.AddComponent<LLCmptAttachment>(att);
                        att.AttachmentID = attachmentID ?? "";
                        att.AttachmentPoint = args.Prim.PrimData.AttachmentPoint;
                        return newEnt;
                    })) {
                    } else {
                        m_log.Log(KLogLevel.DBADERROR, "FAILED CREATION OF NEW ATTACHMENT");
                    }
                    ent.Update(updateFlags);
                } catch (Exception e) {
                    m_log.Log(KLogLevel.DBADERROR, "FAILED CREATION OF NEW ATTACHMENT: " + e.ToString());
                }
            }
            return;
        }
        // ===============================================================
        private void Objects_TerseObjectUpdate(object? sender, OMV.TerseObjectUpdateEventArgs args) {
            if (QueueTilOnline(args.Simulator, CommActionCode.TerseObjectUpdate, sender, args)) return;
            LLRegionContext rcontext = FindRegion(args.Simulator);
            OMV.ObjectMovementUpdate update = args.Update;
            m_statObjTerseUpdate.Event();
            IEntity? updatedEntity = null;
            UpdateCodes updateFlags = 0;
            lock (m_opLock) {
                if (args.Prim.Acceleration != args.Update.Acceleration) updateFlags |= UpdateCodes.Acceleration;
                if (args.Prim.Velocity != args.Update.Velocity) updateFlags |= UpdateCodes.Velocity;
                if (args.Prim.AngularVelocity != args.Update.AngularVelocity) updateFlags |= UpdateCodes.AngularVelocity;
                if (args.Prim.Position != args.Update.Position) updateFlags |= UpdateCodes.Position;
                if (args.Prim.Rotation != args.Update.Rotation) updateFlags |= UpdateCodes.Rotation;
                if (update.Avatar) updateFlags |= UpdateCodes.CollisionPlane;
                if (update.Textures != null) updateFlags |= UpdateCodes.Textures;
                m_log.Log(KLogLevel.DUPDATEDETAIL, "Object update: id={0}, p={1}, r={2}",
                        update.LocalID, update.Position.ToString(), update.Rotation.ToString());

                try {
                    if (args.Prim.ID == OMV.UUID.Zero) {
                        m_log.Log(KLogLevel.DBADERROR, "TerseObjectUpdate: received prim with UUID zero");
                        return;
                    }
                    if (rcontext.TryGetCreateEntityLocalID(args.Prim.LocalID, out updatedEntity, delegate () {
                        // code called to create the entry if it's not found
                        updateFlags |= UpdateCodes.New;
                        updateFlags |= UpdateCodes.Acceleration | UpdateCodes.AngularVelocity | UpdateCodes.Velocity;
                        return InstanceFactory.CreateLLPhysical(GridClient, args.Prim);
                    })) {
                        // new prim created
                        // If this requires special rendering parameters add those parameters
                        // At the moment, the only case is foliage
                        if (args.Prim.PrimData.PCode == OpenMetaverse.PCode.Grass
                                    || args.Prim.PrimData.PCode == OpenMetaverse.PCode.Tree
                                    || args.Prim.PrimData.PCode == OpenMetaverse.PCode.NewTree) {
                            LLCmptSpecialRenderType srt = new LLCmptSpecialRenderType(m_log, updatedEntity, GridClient, rcontext);
                            srt.Type = SpecialRenderTypes.Foliage;
                            srt.FoliageType = args.Prim.PrimData.PCode;
                            srt.TreeType = args.Prim.TreeSpecies;
                            updatedEntity.AddComponent<LLCmptSpecialRenderType>(srt);
                        }
                        // if there are animations for this entity
                        ProcessEntityAnimation(updatedEntity, ref updateFlags, args.Prim.AngularVelocity);
                    }
                    // send updates for this entity updates
                    ProcessEntityUpdates(updatedEntity, updateFlags);
                } catch (Exception e) {
                    m_log.Log(KLogLevel.DBADERROR, "FAILED CREATION OF NEW PRIM: " + e.ToString());
                }
            }

            return;
        }
        // ===============================================================
        private void Objects_ObjectProperties(object? sender, OMV.ObjectPropertiesEventArgs args) {
            m_log.Log(KLogLevel.DUPDATEDETAIL, "Objects_ObjectProperties:");
            m_statObjObjectProperties.Event();
        }
        // ===============================================================
        private void Objects_ObjectPropertiesUpdated(object? sender, OMV.ObjectPropertiesUpdatedEventArgs args) {
            m_log.Log(KLogLevel.DUPDATEDETAIL, "Objects_ObjectPropertiesUpdated:");
            m_statObjObjectPropertiesUpdate.Event();
        }
        // ===============================================================
        public void Objects_AvatarUpdate(object? sender, OMV.AvatarUpdateEventArgs args) {
            if (QueueTilOnline(args.Simulator, CommActionCode.OnAvatarUpdate, sender, args)) return;
            lock (m_opLock) {
                LLRegionContext rcontext = FindRegion(args.Simulator);
                if (!ParentExists(rcontext, args.Avatar.ParentID)) {
                    // if this requires a parent and the parent isn't here yet, queue this operation til later
                    rcontext.RequestLocalID(args.Avatar.ParentID);
                    QueueTilLater(args.Simulator, CommActionCode.OnAvatarUpdate, sender, args);
                    return;
                }
                m_statObjAvatarUpdate.Event();
                m_log.Log(KLogLevel.DUPDATEDETAIL, "Objects_AvatarUpdate: cntl={0}, parent={1}, p={2}, r={3}",
                            args.Avatar.ControlFlags.ToString("x"), args.Avatar.ParentID,
                            args.Avatar.Position, args.Avatar.Rotation);
                UpdateCodes updateFlags = UpdateCodes.Acceleration | UpdateCodes.AngularVelocity
                            | UpdateCodes.Position | UpdateCodes.Rotation | UpdateCodes.Velocity;
                // This is an avatar, assume somethings changed no matter what
                updateFlags |= UpdateCodes.CollisionPlane;

                EntityName avatarEntityName = new EntityNameLL(rcontext.AssetContext, "/Avatar/" + args.Avatar.ID.ToString());

                IEntity? updatedEntity;
                if (!rcontext.Entities.TryGetEntity(avatarEntityName, out updatedEntity)) {
                    m_log.Log(KLogLevel.DUPDATEDETAIL, "AvatarUpdate: creating avatar {0} {1} ({2})",
                        args.Avatar.FirstName, args.Avatar.LastName, args.Avatar.ID);
                    updatedEntity = InstanceFactory.CreateLLAvatar(GridClient, LLAgentConfig);
                    updateFlags |= UpdateCodes.New;
                }
                if (updatedEntity != null) {
                    // created new entity. 
                    updatedEntity.Cmpt<ICmptLocation>().LocalPosition = args.Avatar.Position;
                    updatedEntity.Cmpt<ICmptLocation>().Heading = args.Avatar.Rotation;
                    // We check here if this avatar goes with the agent in the world
                    // If this av is with the agent, make the connection
                    m_log.Log(KLogLevel.DUPDATEDETAIL, "AvatarUpdate: Alid={0}, Clid={1}",
                                            args.Avatar.LocalID, GridClient.Self.LocalID);
                    if (args.Avatar.LocalID == GridClient.Self.LocalID) {
                        m_log.Log(KLogLevel.DUPDATEDETAIL, "AvatarUpdate: associating agent with new avatar");
                        this.MainAgent = updatedEntity as LLEntity;
                    }
                    // send updates for the updated entity
                    ProcessEntityUpdates(updatedEntity, updateFlags);
                }
            }
            return;
        }

        // ===============================================================
        public virtual void Objects_KillObject(object? sender, OMV.KillObjectEventArgs args) {
            if (QueueTilOnline(args.Simulator, CommActionCode.KillObject, sender, args)) return;
            LLRegionContext rcontext = FindRegion(args.Simulator);
            if (rcontext == null) return;
            m_statObjKillObject.Event();
            m_log.Log(KLogLevel.DWORLDDETAIL, "Object killed:");
            try {
                IEntity removedEntity;
                if (rcontext.TryGetEntityLocalID(args.ObjectLocalID, out removedEntity)) {
                    // we need a handle to the objectID
                    IEntityCollection coll;
                    rcontext.Entities.RemoveEntity(removedEntity);
                }
            } catch (Exception e) {
                m_log.Log(KLogLevel.DBADERROR, "FAILED DELETION OF OBJECT: " + e.ToString());
            }
            return;
        }

        // ===============================================================
        public virtual void Avatars_AvatarAppearance(object? sender, OMV.AvatarAppearanceEventArgs args) {
            if (QueueTilOnline(args.Simulator, CommActionCode.OnAvatarAppearance, sender, args)) return;
            LLRegionContext? rcontext = FindRegion(args.Simulator);
            if (rcontext == null) return;
            m_log.Log(KLogLevel.DCOMMDETAIL, "AvatarAppearance: id={0}", args.AvatarID.ToString());
            // the appearance information is stored in the avatar info in libomv
            // We just kick the system to look at it
            lock (m_opLock) {
                EntityName avatarEntityName = new EntityNameLL(rcontext.AssetContext, "/Avatar/" + args.AvatarID.ToString());
                IEntity? ent;
                if (rcontext.TryGetEntity(avatarEntityName, out ent)) {
                    ent?.Update(UpdateCodes.Appearance);
                }
            }
            return;
        }
        // ===============================================================
        /// <summary>
        /// Called when we just log in. We create our agent and put it into the world
        /// </summary>
        public virtual void Comm_OnLoggedIn() {
            m_log.Log(KLogLevel.DWORLD, "Comm_OnLoggedIn:");
            m_World.AddAgent(this.MainAgent);
            // I work by taking LLLP messages and updating the agent
            // The agent will be updated in the world (usually by the viewer)
            // Create the two way communication linkage
            // this.MainAgent.OnUpdated += new AgentUpdatedCallback(Comm_OnAgentUpdated);
        }

        // ===============================================================
        public virtual void Comm_OnLoggedOut() {
            m_log.Log(KLogLevel.DWORLD, "Comm_OnLoggedOut:");
        }

        // ===============================================================
        public virtual void Comm_OnAgentUpdated(IEntity agnt, UpdateCodes what) {
            m_log.Log(KLogLevel.DWORLDDETAIL, "Comm_OnAgentUpdated:");

        }

        // ===============================================================
        // given a simulator. Find the region info that we store the stuff in
        // Note that, if we are not connected, we just return null thus showing our unhappiness.
        public virtual LLRegionContext? FindRegion(OMV.Simulator sim) {
            LLRegionContext? foundRegion = null;
            if (IsConnected) {
                lock (m_regionList) {
                    if (!m_regionList.TryGetValue(sim.ID, out foundRegion)) {
                        // we are connected but doen't have a regionContext for this simulator. Build one.

                        foundRegion = InstanceFactory.CreateLLRegionContext(GridClient, GridsAssetContext, sim);
                        // foundRegion.Name = new EntityNameLL(LoggedInGridName + "/Region/" + sim.Name.Trim());
                        foundRegion.Name = new EntityNameLL(LoggedInGridName + "/" + sim.Name.Trim());

                        var terrain = foundRegion.TerrainInfo;
                        if (terrain != null) {
                            terrain.WaterHeight = sim.WaterHeight;
                            // TODO: copy terrain texture IDs
                        }

                        m_regionList.Add(sim.ID, foundRegion);
                        m_log.Log(KLogLevel.DWORLD, "Creating region context for " + foundRegion.Name);
                    }
                }
            }
            return foundRegion;
        }

        // Use a uniqe test to select a region
        public LLRegionContext? FindRegion(Predicate<LLRegionContext> pred) {
            LLRegionContext? ret = null;
            lock (m_regionList) {
                foreach (var kvp in m_regionList) {
                    if (pred(kvp.Value)) {
                        ret = kvp.Value;
                        break;
                    }
                }
            }
            return ret;
        }

        #region DELAYED REGION MANAGEMENT
        // We get events before the sim comes online. This is a way to queue up those
        // events until we're online.
        public enum CommActionCode {
            RegionStateChange,
            OnObjectDataBlockUpdated,
            OnObjectUpdated,
            TerseObjectUpdate,
            OnAttachmentUpdate,
            KillObject,
            OnAvatarUpdate,
            OnAvatarAppearance
        }

        protected struct ParamBlock {
            public OMV.Simulator sim;
            public CommActionCode cac;
            public object? p1; public object? p2; public object? p3; public object? p4;
            public ParamBlock(OMV.Simulator psim, CommActionCode pcac, object? pp1, object? pp2, object? pp3, object? pp4) {
                sim = psim; cac = pcac; p1 = pp1; p2 = pp2; p3 = pp3; p4 = pp4;
            }
        }
        // ======================================================================
        private void QueueTilLater(OMV.Simulator sim, CommActionCode cac, object? p1) {
            QueueTilLater(sim, cac, p1, null, null, null);
        }

        private void QueueTilLater(OMV.Simulator sim, CommActionCode cac, object? p1, object? p2) {
            QueueTilLater(sim, cac, p1, p2, null, null);
        }

        private void QueueTilLater(OMV.Simulator sim, CommActionCode cac, object? p1, object? p2, object? p3) {
            QueueTilLater(sim, cac, p1, p2, p3, null);
        }

        /// <summary>
        /// Queue the operation to be done later. This is used for waiting for the parent of
        /// a prim. The type of queuing done makes it wait for a default delay before trying
        /// the operation so this, in theory, waits for the parent.
        /// </summary>
        /// <param name="sim"></param>
        /// <param name="cac"></param>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="p3"></param>
        /// <param name="p4"></param>
        private void QueueTilLater(OMV.Simulator sim, CommActionCode cac, object? p1, object? p2, object? p3, object? p4) {
            // m_log.Log(KLogLevel.DCOMMDETAIL, "QueueTilLater: c={0}", cac);
            Object[] parms = { sim, cac, p1, p2, p3, p4 };
            m_waitTilLater.DoLaterInitialDelay(QueueTilLaterDoIt, parms);
            return;
        }

        private bool QueueTilLaterDoIt(DoLaterBase dlb, Object p) {
            Object[] parms = (Object[])p;
            CommActionCode cac = (CommActionCode)parms[1];
            // m_log.Log(KLogLevel.DCOMMDETAIL, "QueueTilLaterDoIt: c={0}", cac);
            RegionAction(cac, parms[2], parms[3], parms[4], parms[5]);
            return true;
        }

        // ======================================================================
        private bool QueueTilOnline(OMV.Simulator sim, CommActionCode cac, object? p1) {
            return QueueTilOnline(sim, cac, p1, null, null, null);
        }

        private bool QueueTilOnline(OMV.Simulator sim, CommActionCode cac, object? p1, object? p2) {
            return QueueTilOnline(sim, cac, p1, p2, null, null);
        }

        private bool QueueTilOnline(OMV.Simulator sim, CommActionCode cac, object? p1, object? p2, object? p3) {
            return QueueTilOnline(sim, cac, p1, p2, p3, null);
        }

        /// <summary>
        ///  Check to see if this action can happen now or has to be queued for later.
        /// </summary>
        /// <param name="rcontext"></param>
        /// <param name="cac"></param>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="p3"></param>
        /// <param name="p4"></param>
        /// <returns>true if the action was queued, false if the action should be done</returns>
        private bool QueueTilOnline(OMV.Simulator sim, CommActionCode cac, object? p1, object? p2, object? p3, object? p4) {
            bool ret = false;
            lock (m_waitTilOnline) {
                IRegionContext? rcontext = FindRegion(sim);
                if (rcontext != null && rcontext.State.isOnline) {
                    // not queuing until later
                    ret = false;
                } else {
                    ParamBlock pb = new ParamBlock(sim, cac, p1, p2, p3, p4);
                    m_waitTilOnline.Add(pb);
                    // return that we queued the action
                    ret = true;
                }
            }
            return ret;
        }

        private void DoAnyWaitingEvents(OMV.Simulator sim) {
            m_log.Log(KLogLevel.DCOMMDETAIL, "DoAnyWaitingEvents: examining {0} queued events", m_waitTilOnline.Count);
            List<ParamBlock> m_queuedActions = new List<ParamBlock>();
            lock (m_waitTilOnline) {
                // get out all of teh actions saved for this sim
                foreach (ParamBlock pb in m_waitTilOnline) {
                    if (pb.sim == sim) {
                        m_queuedActions.Add(pb);
                    }
                }
                // remove the entries for the sim
                foreach (ParamBlock pb in m_queuedActions) {
                    m_waitTilOnline.Remove(pb);
                }
            }
            // process each of the actions. If they should stay queued, they will get requeued
            m_log.Log(KLogLevel.DCOMMDETAIL, "DoAnyWaitingEvents: processing {0} queued events", m_queuedActions.Count);
            foreach (ParamBlock pb in m_queuedActions) {
                RegionAction(pb.cac, pb.p1, pb.p2, pb.p3, pb.p4);
            }
        }

        public void RegionAction(CommActionCode cac, Object p1, Object p2, Object p3, Object p4) {
            try {
                switch (cac) {
                    case CommActionCode.RegionStateChange:
                        // m_log.Log(KLogLevel.DCOMMDETAIL, "RegionAction: RegionStateChange");
                        // NOTE that this goes straight to the status update routine
                        ((RegionContextBase)p1).Update((World.UpdateCodes)p2);
                        break;
                    case CommActionCode.OnObjectDataBlockUpdated:
                        // m_log.Log(KLogLevel.DCOMMDETAIL, "RegionAction: OnObjectDataBlockUpdated");
                        Objects_ObjectDataBlockUpdate(p1, (OMV.ObjectDataBlockUpdateEventArgs)p2);
                        break;
                    case CommActionCode.OnObjectUpdated:
                        // m_log.Log(KLogLevel.DCOMMDETAIL, "RegionAction: OnObjectUpdated");
                        // Objects_OnObjectUpdated((OMV.Simulator)p1, (OMV.ObjectUpdate)p2, (ulong)p3, (ushort)p4);
                        Objects_ObjectUpdate(p1, (OMV.PrimEventArgs)p2);
                        break;
                    case CommActionCode.TerseObjectUpdate:
                        // m_log.Log(KLogLevel.DCOMMDETAIL, "RegionAction: TerseObjectUpdate");
                        Objects_TerseObjectUpdate(p1, (OMV.TerseObjectUpdateEventArgs)p2);
                        break;
                    case CommActionCode.OnAttachmentUpdate:
                        // m_log.Log(KLogLevel.DCOMMDETAIL, "RegionAction: OnAttachmentUpdated");
                        Objects_AttachmentUpdate(p1, (OMV.PrimEventArgs)p2);
                        break;
                    case CommActionCode.KillObject:
                        // m_log.Log(KLogLevel.DCOMMDETAIL, "RegionAction: KillObject");
                        Objects_KillObject(p1, (OMV.KillObjectEventArgs)p2);
                        break;
                    case CommActionCode.OnAvatarUpdate:
                        // m_log.Log(KLogLevel.DCOMMDETAIL, "RegionAction: AvatarUpdate");
                        Objects_AvatarUpdate(p1, (OMV.AvatarUpdateEventArgs)p2);
                        break;
                    case CommActionCode.OnAvatarAppearance:
                        // m_log.Log(KLogLevel.DCOMMDETAIL, "RegionAction: AvatarAppearance");
                        Avatars_AvatarAppearance(p1, (OMV.AvatarAppearanceEventArgs)p2);
                        break;
                    default:
                        break;
                }
            } catch (Exception e) {
                m_log.Log(KLogLevel.DCOMMDETAIL, "RegionAction: FAILURE PROCESSING {0}: {1}", cac, e);
            }
        }
        #endregion DELAYED REGION MANAGEMENT



    }
}
