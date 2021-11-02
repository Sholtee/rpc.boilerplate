using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

using Microsoft.Extensions.Logging;

using ServiceStack.Data;

using Solti.Utils.DI;
using Solti.Utils.DI.Interfaces;
using Solti.Utils.Rpc;
using Solti.Utils.Rpc.Hosting;
using Solti.Utils.Rpc.Interfaces;

namespace Server
{
    using DAL;
    using DAL.API;

    using Modules;
    using Modules.API;

    using Services;
    using Services.API;

    public sealed class AppHost : AppHostBase
    {
        private IConfig Config { get; }

        public AppHost()
        {
            Config = Services.Config.Read("config.json");
            Name = "MyApp";
#if RELEASE
            AutoStart = true;
#endif
        }

        private sealed class LowerCasePolicy : JsonNamingPolicy
        {
            [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase")]
            public override string ConvertName(string name) => name.ToLowerInvariant();
        }

        public override void OnBuildService(RpcServiceBuilder serviceBuilder)
        {
            if (serviceBuilder is null)
                throw new ArgumentNullException(nameof(serviceBuilder));

            serviceBuilder
                .ConfigureWebService(new WebServiceDescriptor
                 {
                     Url = Config.Server.Host,
                     AllowedOrigins = new List<string>(Config.Server.AllowedOrigins ?? Array.Empty<string>())
                 })
                .ConfigureSerializer(new JsonSerializerOptions
                {
                    PropertyNamingPolicy = new LowerCasePolicy()
                })
                .ConfigureModules(registry => registry
                    .Register<IUserManager, UserManager>())
                .ConfigureServices(svcs => svcs
                    .Instance<IConfig>(Config)
                    .Provider<IDbConnectionFactory, MySqlDbConnectionFactoryProvider>(Lifetime.Singleton)
                    .Provider<IDbConnection, SqlConnectionProvider>(Lifetime.Scoped)
                    .Factory<ILogger>(i => TraceLogger.Create<AppHost>(), Lifetime.Singleton)
                    //.Service<ICache, RedisCache>(Lifetime.Scoped)
                    .Service<ICache, MemoryCache>(Lifetime.Singleton)
                    .Service<IRoleManager, RoleManager>(Lifetime.Scoped)
                    .Service<IAsyncRoleManager, RoleManager>(Lifetime.Scoped)
                    .Service<IUserRepository, SqlUserRepository>(Lifetime.Scoped));
        }
        public override void OnInstall()
        {
            base.OnInstall();

            using IScopeFactory scopeFactory = ScopeFactory.Create(svcs => svcs
                .Instance<IConfig>(Config)
                .Instance<IReadOnlyList<string>>("CommandLineArgs", Environment.GetCommandLineArgs())
                .Provider<IDbConnectionFactory, MySqlDbConnectionFactoryProvider>(Lifetime.Singleton)
                .Provider<IDbConnection, SqlConnectionProvider>(Lifetime.Scoped)
                .Factory<ILogger>(i => TraceLogger.Create<AppHost>(), Lifetime.Singleton)
                .Service<IDbSchemaManager, SqlDbSchemaManager>(Lifetime.Scoped)
                .Service<IUserRepository, SqlUserRepository>(Lifetime.Scoped));

            using IInjector injector = scopeFactory.CreateScope();

            injector.Instantiate<Installer>().Run(GetType().Assembly);
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
                //
            }

            Environment.Exit(-1);
        }
    }
}
