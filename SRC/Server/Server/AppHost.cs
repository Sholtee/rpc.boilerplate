using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

using ServiceStack.Data;

using Solti.Utils.DI.Interfaces;
using Solti.Utils.Primitives;
using Solti.Utils.Rpc.Interfaces;
using Solti.Utils.Rpc.Hosting;

namespace Server
{
    using DAL;
    using DAL.API;

    using Modules;
    using Modules.API;

    using Services;
    using Services.API;

    public class AppHost : AppHostBase
    {
        private IConfig Config { get; }

        public override string Name => "Erettsegi";

        public override string Url => Config.Server.Host;

        public AppHost()
        {
            Config = Services.Config.Read("config.json");
            Config.Server.AllowedOrigins?.ForEach((origin, _) => RpcService.AllowedOrigins.Add(origin));

            RpcService.SerializerOptions.PropertyNamingPolicy = new LowerCasePolicy();
#if RELEASE
            AutoStart = true;
#endif
        }

        private sealed class LowerCasePolicy : JsonNamingPolicy
        {
            [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase")]
            public override string ConvertName(string name) => name.ToLowerInvariant();
        }

        public override void OnRegisterModules(IModuleRegistry registry)
        {
            if (registry is null)
                throw new ArgumentNullException(nameof(registry));

            base.OnRegisterModules(registry);

            registry.Register<IUserManager, UserManager>();
        }

        public override void OnInstall()
        {
            base.OnInstall();

            using IInjector injector = CreateInjector();

            //
            // These services are required for installation only.
            //

            injector
                .UnderlyingContainer
                .Service<IDbSchemaManager, SqlDbSchemaManager>(Lifetime.Scoped)
                .Service<IInstaller, Installer>(Lifetime.Scoped);

            injector
                .Get<IInstaller>()
                .Run(GetType().Assembly);
        }

        public override void OnUnhandledException(Exception ex)
        {
            if (ex is null)
                throw new ArgumentNullException(nameof(ex));

            base.OnUnhandledException(ex);

            string msg = ex.Message;
            if (ex is ValidationException validationException)
                msg += $"{Environment.NewLine}Target: {validationException.TargetName}";

            try
            {
                ConsoleColor oldColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(msg);
                Console.ForegroundColor = oldColor;
            }
            #pragma warning disable CA1031 // Do not catch general exception types
            catch
            #pragma warning restore CA1031
            {
                //
                // We have no Console
                // //
            }

            Environment.Exit(-1);
        }

        public override void OnRegisterServices(IServiceContainer container)
        {
            if (container is null)
                throw new ArgumentNullException(nameof(container));

            base.OnRegisterServices(container);

            container
                .Instance<IConfig>(Config)
                .Provider<IDbConnectionFactory, MySqlDbConnectionFactoryProvider>(Lifetime.Singleton)
                .Provider<IDbConnection, SqlConnectionProvider>(Lifetime.Scoped)
                //.Service<ICache, RedisCache>(Lifetime.Scoped)
                .Service<ICache, MemoryCache>(Lifetime.Singleton)
                .Service<IRoleManager, RoleManager>(Lifetime.Scoped)
                .Service<IAsyncRoleManager, RoleManager>(Lifetime.Scoped)
                .Service<IUserRepository, SqlUserRepository>(Lifetime.Scoped);
        }
    }
}
