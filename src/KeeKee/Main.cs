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
using NLog.Extensions.Logging;

using KeeKee.Framework.Config;
using KeeKee.Framework.Logging;
using KeeKee.Rest;
using Microsoft.Extensions.Options;
using KeeKee.Framework;

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
                     // Configuration binding
                     RegisterConfigurationOptions(context, services);
                     // Core Services
                     RegisterServices(context, services);
                     // Infrastructure Services
                     //     e.g., database, messaging, etc.
                     // Application Services
                     //        e.g., jobrunners, etc.
                     // Background Workers
                     //    e.g., hosted services, etc.
                     RegisterWorkers(context, services);


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


        private static void RegisterConfigurationOptions(HostBuilderContext pContext, IServiceCollection pServices) {
            var config = pContext.Configuration;
            // pServices.AddOptions<MyOptions>().Bind(config.GetSection("MyOptions"));

            // Add IOptions config for the individual config sections
            // This will allow someone to inject IOptions<CodeConfig> to get the config params.
            //     Using the IOptions interface allows the config to be changed at runtime
            //     (see IOptionsSnapshot for more info).
            pServices.Configure<KeeKeeConfig>(config.GetSection(KeeKeeConfig.subSectionName));
            pServices.Configure<KLoggerConfig>(config.GetSection(KLoggerConfig.subSectionName));
            pServices.Configure<RestManagerConfig>(config.GetSection(RestManagerConfig.subsectionName));

        }

        private static void RegisterServices(HostBuilderContext pContext, IServiceCollection pServices) {
            // lots of _serviceCollection.AddSingleton<IInterface, InterfaceClass>();
            // Register services here if needed

            // pServices.AddSingleton<IMyService, MyService>();

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

            // The KLogger wrapper around ILogger
            pServices.AddTransient(typeof(ILogger<>), typeof(KLogger<>));

            pServices.AddTransient<IInstanceFactory, InstanceFactory>();
            pServices.AddTransient<RestHandlerFactory, RestHandlerFactory>();


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


        }

        private static void RegisterWorkers(HostBuilderContext pContext, IServiceCollection pServices) {
            // lots of _serviceCollection.AddHostedService<WorkerClass>();
            // Register background workers here if needed
            // pServices.AddHostedService<BackgroundWorker>();
            pServices.AddHostedService<RestManager>();
        }
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