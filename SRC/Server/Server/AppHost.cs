using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

using Microsoft.Extensions.Logging;

using Solti.Utils.DI.Interfaces;
using Solti.Utils.Primitives;
using Solti.Utils.Rpc;
using Solti.Utils.Rpc.Hosting;
using Solti.Utils.Rpc.Interfaces;
using Solti.Utils.Rpc.Internals;
using Solti.Utils.Rpc.Pipeline;
using Solti.Utils.Rpc.Servers;

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

            public static LowerCasePolicy Instance { get; } = new LowerCasePolicy();
        }

        public override void OnConfigure(WebServiceBuilder serviceBuilder)
        {
            if (serviceBuilder is null)
                throw new ArgumentNullException(nameof(serviceBuilder));

            ServerConfig serverConfig = new Config<ServerConfig>(configFile).Value;

            serviceBuilder
                .ConfigureBackend(_ => new HttpListenerBackend(serverConfig.Host)
                {
                    ReserveUrl = true
                })
                .ConfigureRpcService((RequestHandlerBuilder conf) =>
                {
                    switch (conf)
                    {
                        case Solti.Utils.Rpc.Pipeline.Modules modules:
                            modules
                                .ConfigureSerializer(_ =>
                                {
                                    JsonSerializerBackend serializer = new();
                                    serializer.Options.PropertyNamingPolicy = LowerCasePolicy.Instance;
                                    return serializer;
                                })
                                .Register<IUserManager, UserManager>();
                            break;
                        case RpcAccessControl rpcAc:
                            foreach (string origin in serverConfig.AllowedOrigins)
                            {
                                rpcAc.AllowedOrigins.Add(origin);
                            }
                            break;
                    }
                }, useDefaultLogger: false)
                .ConfigureServices(svcs => { /*WebService exclusive services go here*/ });
        }

        public override void OnConfigureServices(IServiceCollection services) => services
            .Factory<ILogger>(_ => ConsoleLogger.Create<AppHost>(), Lifetime.Scoped) // cannot be Singleton due to log scopes
            .Provider<IDbConnection, MySqlDbConnectionProvider>(Lifetime.Scoped)
            .Provider<IDbConnection, SQLiteDbConnectionProvider>(SQLiteDbConnectionProvider.ServiceName, Lifetime.Scoped)
            .Service(typeof(IConfig<>), typeof(Config<>), new Dictionary<string, object?> { [nameof(configFile)] = configFile }, Lifetime.Singleton)
            .Service<IDbSchemaManager, SqlDbSchemaManager>(SQLiteDbConnectionProvider.ServiceName, explicitArgs: new Dictionary<string, object?> { [dbTag] = SQLiteDbConnectionProvider.ServiceName }, Lifetime.Singleton)
            .Service<IDbSchemaManager, SqlDbSchemaManager>(explicitArgs: new Dictionary<string, object?> { [dbTag] = null }, Lifetime.Singleton)
            .Service<ICache, RedisCache>(Lifetime.Scoped)
            .Service<IRoleManager, RoleManager>(Lifetime.Scoped)
            .Service<ISessionRepository, SqlSessionRepository>(Lifetime.Scoped)
            .Service<IUserRepository, SqlUserRepository>(Lifetime.Scoped)
            .Service<IInstaller, Installer>(Lifetime.Scoped);

        public override void OnBuilt()
        {
            //
            // Since this handler is called right after the OnConfigureServices() method, we cannot use the InvokeInScope() helper
            //

            using IInjector scope = WebService!.ScopeFactory.CreateScope();

            IDbSchemaManager schemaManager = scope.Get<IDbSchemaManager>(SQLiteDbConnectionProvider.ServiceName);
            schemaManager.Initialize();
        }

        public override void OnInstall() => InvokeInScope((IInjector scope) => scope
            .Get<IInstaller>()
            .Install(GetParsedArguments<InstallArguments>()));

        [Verb("status")]
        public void OnPrintStatus() => InvokeInScope((IInjector scope) => Console.Out.WriteLine(scope.Get<IInstaller>().Status));

        [Verb("migrate")]
        public void OnMigrate() => InvokeInScope((IInjector scope) => scope
            .Get<IInstaller>()
            .Migrate()
            .ForEach((status, _) => Console.Out.WriteLine(status)));

        public override void OnUnhandledException(Exception ex)
        {
            string msg = ex?.Message ?? Resources.UNKNOWN_ERROR;
            if (ex is ValidationException validationException)
                msg += $"{Environment.NewLine}{Resources.TARGET}: {validationException.TargetName}";

            Console.Error.WriteLine(msg);
        }
    }
}
