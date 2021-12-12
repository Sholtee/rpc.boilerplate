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
using Solti.Utils.Rpc.Internals;

namespace Server
{
    using DAL;
    using DAL.API;

    using Loggers;

    using Modules;
    using Modules.API;

    using Services;
    using Services.API;

    public sealed class AppHost : AppHostBase
    {
        private IConfig Config { get; }

        public AppHost(IReadOnlyList<string> args) : base(args) => Config = Services.Config.Read("config.json");

        private sealed class LowerCasePolicy : JsonNamingPolicy
        {
            [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase")]
            public override string ConvertName(string name) => name.ToLowerInvariant();
        }

        private void InvokeInstaller(Action<IInstaller> invocation)
        {
            using IScopeFactory scopeFactory = ScopeFactory.Create(svcs => svcs
                .Instance<IConfig>(Config)
                .Instance<ILogger>(ConsoleLogger.Create<AppHost>())
                .Provider<IDbConnectionFactory, MySqlDbConnectionFactoryProvider>(Lifetime.Singleton)
                .Provider<IDbConnection, SqlConnectionProvider>(Lifetime.Scoped)
                .Service<IDbSchemaManager, SqlDbSchemaManager>(Lifetime.Scoped)
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
                    .Instance<ILogger>(ConsoleLogger.Create<AppHost>())
                    .Provider<IDbConnectionFactory, MySqlDbConnectionFactoryProvider>(Lifetime.Singleton)
                    .Provider<IDbConnection, SqlConnectionProvider>(Lifetime.Scoped)
                    //.Service<ICache, RedisCache>(Lifetime.Scoped)
                    .Service<ICache, MemoryCache>(Lifetime.Singleton)
                    .Service<IRoleManager, RoleManager>(Lifetime.Scoped)
                    .Service<IUserRepository, SqlUserRepository>(Lifetime.Scoped));
        }

        public override void OnInstall()
        {
            base.OnInstall();

            InvokeInstaller(installer => installer.Install(GetParsedArguments<InstallArguments>()));
        }

        public override void OnUnhandledException(Exception ex)
        {
            string msg = ex?.Message ?? "Unknown error";
            if (ex is ValidationException validationException)
                msg += $"{Environment.NewLine}Target: {validationException.TargetName}";

            Console.Error.WriteLine(msg);
        }

        [Verb("status")]
        public void OnPrintStatus() => InvokeInstaller(installer => Console.Out.WriteLine(installer.Status));
    }
}
