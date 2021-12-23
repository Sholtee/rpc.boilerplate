using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

using Microsoft.Extensions.Logging;

using Solti.Utils.DI;
using Solti.Utils.DI.Interfaces;
using Solti.Utils.Primitives;
using Solti.Utils.Rpc;
using Solti.Utils.Rpc.Hosting;
using Solti.Utils.Rpc.Interfaces;
using Solti.Utils.Rpc.Internals;

namespace Server
{
    using DAL;
    using DAL.API;

    using Loggers;

    using Modules;
    using Modules.API;

    using Properties;

    using Services;
    using Services.API;

    public sealed class AppHost : AppHostBase
    {
        const string
            dbTag = nameof(dbTag),
            configFile = "config.json";

        public AppHost(IReadOnlyList<string> args) : base(args) { }

        private sealed class LowerCasePolicy : JsonNamingPolicy
        {
            [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase")]
            public override string ConvertName(string name) => name.ToLowerInvariant();
        }

        private static void InvokeInstaller(Action<IInstaller> invocation)
        {
            using IScopeFactory scopeFactory = ScopeFactory.Create(svcs => svcs
                .Instance<ILogger>(TraceLogger.Create<AppHost>()) // required due to Logger aspects
                .Provider<IDbConnection, MySqlDbConnectionProvider>(Lifetime.Scoped)
                .Service(typeof(IConfig<>), typeof(Config<>), new Dictionary<string, object?> { [nameof(configFile)] = configFile }, Lifetime.Singleton)
                .Service<IDbSchemaManager, SqlDbSchemaManager>(explicitArgs: new Dictionary<string, object?> { [dbTag] = null }, Lifetime.Singleton)
                .Service<IUserRepository, SqlUserRepository>(Lifetime.Scoped)
                .Service<IInstaller, Installer>(Lifetime.Scoped));

            using IInjector injector = scopeFactory.CreateScope();

            invocation(injector.Get<IInstaller>());
        }

        public override void OnConfigure(RpcServiceBuilder serviceBuilder)
        {
            if (serviceBuilder is null)
                throw new ArgumentNullException(nameof(serviceBuilder));

            base.OnConfigure(serviceBuilder);

            ServerConfig serverConfig = new Config<ServerConfig>(configFile).Value;

            serviceBuilder
                .ConfigureWebService(new WebServiceDescriptor
                 {
                     Url = serverConfig.Host,
                     AllowedOrigins = new List<string>(serverConfig.AllowedOrigins ?? Array.Empty<string>())
                 })
                .ConfigureSerializer(new JsonSerializerOptions
                {
                    PropertyNamingPolicy = new LowerCasePolicy()
                })
                .ConfigureModules(registry => registry
                    .Register<IUserManager, UserManager>())
                .ConfigureServices(svcs => svcs
                    .Instance<ILogger>(ConsoleLogger.Create<AppHost>())
                    .Provider<IDbConnection, MySqlDbConnectionProvider>(Lifetime.Scoped)
                    .Provider<IDbConnection, SQLiteDbConnectionProvider>(SQLiteDbConnectionProvider.ServiceName, Lifetime.Scoped)
                    .Service(typeof(IConfig<>), typeof(Config<>), new Dictionary<string, object?> { [nameof(configFile)] = configFile }, Lifetime.Singleton)
                    .Service<IDbSchemaManager, SqlDbSchemaManager>(SQLiteDbConnectionProvider.ServiceName, explicitArgs: new Dictionary<string, object?> { [dbTag] = SQLiteDbConnectionProvider.ServiceName }, Lifetime.Singleton)
                    .Service<ICache, RedisCache>(Lifetime.Scoped)
                    .Service<IRoleManager, RoleManager>(Lifetime.Scoped)
                    .Service<ISessionRepository, SqlSessionRepository>(Lifetime.Scoped)
                    .Service<IUserRepository, SqlUserRepository>(Lifetime.Scoped));
        }

        public override void OnBuilt()
        {
            base.OnBuilt();

            using IInjector injector = RpcService!.ScopeFactory.CreateScope();

            IDbSchemaManager schemaManager = injector.Get<IDbSchemaManager>(SQLiteDbConnectionProvider.ServiceName);
            schemaManager.Initialize();
        }

        public override void OnInstall()
        {
            base.OnInstall();

            InvokeInstaller(installer => installer.Install(GetParsedArguments<InstallArguments>()));
        }

        public override void OnUnhandledException(Exception ex)
        {
            string msg = ex?.Message ?? Resources.UNKNOWN_ERROR;
            if (ex is ValidationException validationException)
                msg += $"{Environment.NewLine}{Resources.TARGET}: {validationException.TargetName}";

            Console.Error.WriteLine(msg);
        }

        [Verb("status"), SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Verb attribute must be placed on an instance method")]
        public void OnPrintStatus() => InvokeInstaller(installer => Console.Out.WriteLine(installer.Status));

        [Verb("migrate"), SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Verb attribute must be placed on an instance method")]
        public void OnMigrate() => InvokeInstaller(installer => installer
            .Migrate()
            .ForEach((status, _) => Console.Out.WriteLine(status)));
    }
}
