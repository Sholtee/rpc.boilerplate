using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

using ServiceStack.Data;

using Solti.Utils.DI;
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
            Installer installer = injector.Instantiate<Installer>();

            try
            {
                installer.Run(typeof(DAL.User).Assembly);
            }
            #pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception e)
            #pragma warning restore CA1031
            {
                Console.Error.WriteLine(e.Message);
                Environment.Exit(-1);
            }
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
                //.Service<ICache, MemoryCache>(Lifetime.Singleton)
                .Service<IRoleManager, RoleManager>(Lifetime.Scoped)
                .Service<IAsyncRoleManager, RoleManager>(Lifetime.Scoped)
                .Service<IDbSchemaManager, SqlDbSchemaManager>(Lifetime.Scoped)
                .Service<IUserRepository, SqlUserRepository>(Lifetime.Scoped);
        }
    }
}
