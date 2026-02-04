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

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NLog.Extensions.Logging;

using KeeKee.Config;
using KeeKee.Comm.LLLP;
using KeeKee.Contexts;
using KeeKee.Entity;
using KeeKee.Framework.Logging;
using KeeKee.Rest;
using KeeKee.Renderer;
// using KeeKee.Renderer.OGL;
using KeeKee.Renderer.Map;
using KeeKee.Framework;
using KeeKee.World;
using KeeKee.World.LL;
using KeeKee.Framework.WorkQueue;

using OMV = OpenMetaverse;
using OMVSD = OpenMetaverse.StructuredData;
using OMVR = OpenMetaverse.Rendering;
using KeeKee.Comm;

namespace KeeKee {

    public partial class KeeKeeMain {

        public static IHost? KeeKeeHost { get; private set; } = default!;

        public static CancellationTokenSource GlobalCTS { get; } = new CancellationTokenSource();

        private static KLogger<KeeKeeMain>? m_log;
        public static KLogger<KeeKeeMain> Log {
            get {
                if (m_log == null) {
                    throw new ApplicationException("KeeKeeMain.Log accessed before initialization.");
                }
                return m_log;
            }
        }

        public static IOptions<KeeKeeConfig> GetKeeKeeConfig {
            get {
                return GetService<IOptions<KeeKeeConfig>>();
            }
        }

        /// <summary>
        /// Gets an instance of <typeparamref name="T"/>
        /// Will throw if its unable to provide a <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        /// <exception cref="ApplicationException"></exception>
        public static T GetService<T>() where T : notnull {
            return KeeKeeHost!.Services.GetRequiredService<T>()
                ?? throw new ApplicationException($"There requested type {typeof(T).FullName} could not be provided.");
        }


        public static async Task Main(string[] args) {

            KeeKeeHost = Host.CreateDefaultBuilder(args)
                 .ConfigureAppConfiguration((context, config) => {
                     // CreateDefaultBuilder already adds 'appsettings.json',
                     //     'appsettings.Development.json', and environment variables.

                     // Add the default logging config first so other configs can override it.
                     config.AddJsonFile("KeeKee.json", optional: true, reloadOnChange: true);
                     config.AddJsonFile("Grids.json", optional: true, reloadOnChange: true);
                     config.AddEnvironmentVariables("KeeKee_");
                     // re-add command line args so they override other settings
                     config.AddCommandLine(Environment.GetCommandLineArgs());
                 })
                 .ConfigureLogging(logging => {
                     // Remove all the MS stuff and use NLog
                     logging.ClearProviders();
                     logging.AddNLog();
                     // See
                     // https://github.com/NLog/NLog.Extensions.Logging/wiki/NLog-configuration-with-appsettings.json
                     // for more details on NLog configuration using appsettings.json.
                 })
                 .ConfigureServices((context, services) => {
                     services.Configure<KeeKeeConfig>(context.Configuration.GetSection(KeeKeeConfig.subSectionName));
                     services.AddSingleton<IInstanceFactory, InstanceFactory>();
                     services.Configure<GridConfig>(context.Configuration.GetSection(GridConfig.subSectionName));

                     // Collections and collection managers available for services
                     services.AddSingleton<Grids>();
                     services.AddSingleton<UserPersistantParams>();
                     services.AddSingleton<WorkQueueManager>();
                     services.AddHostedService(sp => sp.GetRequiredService<WorkQueueManager>());
                     services.AddTransient<EntityCollection>();
                     services.AddTransient<RegionState>();

                     // Logger and KLogger wrapper for base logger
                     services.Configure<KLoggerConfig>(context.Configuration.GetSection(KLoggerConfig.subSectionName));
                     services.AddTransient(typeof(KLogger<>));

                     // REST services: provides REST interface for services. RestHandlerFactory creates handlers for each access point.
                     services.Configure<RestManagerConfig>(context.Configuration.GetSection(RestManagerConfig.subSectionName));
                     services.AddTransient<RestHandlerFactory, RestHandlerFactory>();
                     services.AddTransient<RestHandlerUI, RestHandlerUI>();
                     services.AddTransient<RestHandlerStatic, RestHandlerStatic>();
                     services.AddSingleton<RestManager>();
                     services.AddHostedService(sp => sp.GetRequiredService<RestManager>());

                     // Communication services
                     services.Configure<CommConfig>(context.Configuration.GetSection(CommConfig.subSectionName));
                     services.AddTransient<RestHandlerLogin>();
                     services.AddTransient<RestHandlerLogout>();
                     services.AddTransient<RestHandlerTeleport>();
                     services.AddTransient<RestHandlerExit>();
                     services.AddTransient<RestHandlerChat>();
                     services.AddSingleton<LoadWorldObjects>();
                     services.AddSingleton<AssetFetcher>();
                     services.AddSingleton<LLGridClient>();
                     services.AddSingleton<ICommProvider, CommLLLP>();
                     services.AddSingleton<CommLLLP>();
                     services.AddSingleton<CommLLLPRest>();
                     services.AddHostedService(sp => sp.GetRequiredService<CommLLLP>());
                     services.AddHostedService(sp => sp.GetRequiredService<CommLLLPRest>());

                     // World services using LL implementations
                     services.Configure<WorldConfig>(context.Configuration.GetSection(WorldConfig.subSectionName));
                     services.Configure<LLAgentConfig>(context.Configuration.GetSection(LLAgentConfig.subSectionName));
                     services.Configure<AssetConfig>(context.Configuration.GetSection(AssetConfig.subSectionName));
                     services.AddTransient<ILLInstanceFactory, LLInstanceFactory>();
                     services.AddTransient<IEntity, LLEntity>();
                     services.AddTransient<IRegionContext, LLRegionContext>();
                     services.AddTransient<IAssetContext, LLAssetContext>();
                     services.AddTransient<ITerrainInfo, LLTerrainInfo>();

                     // services.AddTransient<IAnimation, LLAnimation>();
                     services.AddSingleton<IWorld, World.World>();
                     services.AddTransient<RegionState>();

                     // Renderer services
                     services.Configure<RendererConfig>(context.Configuration.GetSection(RendererConfig.subSectionName));
                     services.Configure<RendererOGLConfig>(context.Configuration.GetSection(RendererOGLConfig.subSectionName));
                     // services.AddSingleton<RendererOGL>();
                     services.AddSingleton<RendererMap>();
                     // Select the render provider based on configuration
                     services.AddSingleton<IRenderProvider>(sp => {
                         var provider = context.Configuration.GetValue<string>("Renderer:RenderProvider") ?? "OGL";
                         return provider.ToLowerInvariant() switch {
                             // "ogl" => sp.GetRequiredService<RendererOGL>(),
                             "map" => sp.GetRequiredService<RendererMap>(),
                             _ => throw new ApplicationException($"Unknown RenderProvider provider '{provider}'.")
                         };
                     });
                     services.AddTransient<OMVR.IRendering, MeshmerizerR>();
                     // services.AddHostedService<ViewOGL>();

                     // The user interface
                     services.AddSingleton<IUserInterfaceProvider, UserInterfaceCommon>();

                     // KeeKee.Rest, IModule
                     // KeeKee.Comm, ICommProvider
                     // KeeKee.World, IWorld
                     // KeeKee.View, IViewProvider
                     // KeeKee.View, IUserInterfaceProvider
                     // KeeKee.Renderer.OGL, IRendererProvider
                     // KeeKee.Comm.LLLP, IRestUser
                     // KeeKee.View, IViewSplash
                     // KeeKee.View, IViewAvatar
                     // KeeKee.View, IViewChat
                     // KeeKee.View, KeeKee.Renderer.OGL.IViewOGL
                     // KeeKee.View, IRegionTrackerProvider
                     // KeeKee.World.Services, IAvatarTrackerService
                     // KeeKee.Comm.LLLP, Comm.IChatProvider

                     // Asset Server example choosing implementation based on config
                     // pServices.AddSingleton<IAssetServer>(sp => {
                     //     var provider = config.GetValue<string>("AssetServer:Provider") ?? "LLLP";
                     //     OR var provider = sp.GetRequiredService<IOptions<IAssetServerOptions>>().Value.Provider ?? "LLLP";
                     //     return provider.ToLowerInvariant() switch {
                     //         "lllp" => services.AddSingleton<IAssetServer, LllpAssetServer>(),
                     //         "rest" => services.AddSingleton<IAssetServer, RestAssetServer>(),
                     //         _ => throw new ApplicationException($"Unknown AssetServer provider '{provider}'.")
                     //     };
                     // });


                 })
                 .Build();

            m_log = KeeKeeMain.GetService<KLogger<KeeKeeMain>>();

            IOptions<KeeKeeConfig> keeKeeConfig = GetKeeKeeConfig;

            LogConfigurationComplete(m_log);

            await KeeKeeHost.RunAsync(GlobalCTS.Token);

            LogShutdown(m_log);
        }

        // The following are examples of source-generated logging methods.
        [LoggerMessage(0, LogLevel.Information, "KeeKee application starting up.")]
        public static partial void LogStartup(ILogger logger);
        [LoggerMessage(0, LogLevel.Information, "KeeKee application configuration complete.")]
        public static partial void LogConfigurationComplete(ILogger logger);
        [LoggerMessage(0, LogLevel.Information, "KeeKee application shutting down.")]
        public static partial void LogShutdown(ILogger logger);

    }

    /*
    private static string Invocation() {
            return @"Invocation:
View a virtual world with KeeKee.
INVOCATION:
KeeKee 
        --first user
        --last user
        --password password
        --grid gridname
        --loginuri loginuri
        --configFile filename
        --modulesFile filename
        --cache cacheDirectory
        --param parameter:value
        --debug
";
    }
    */

}