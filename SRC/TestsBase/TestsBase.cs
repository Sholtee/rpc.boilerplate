using System.Collections.Generic;
using System.Data;

using Microsoft.Extensions.Logging;
using NUnit.Framework;

using Solti.Utils.DI.Interfaces;

namespace Tests.Base
{
    using Server.Loggers;
    using Services.API;
    using Services;

    public class TestsBase: Debuggable
    {
        public IScopeFactory? ScopeFactory { get; private set; }

        public IInjector? Injector { get; private set; }

        [OneTimeSetUp]
        public void OneTimeSetup() => ScopeFactory = Solti.Utils.DI.ScopeFactory.Create(svcs =>
        {
            svcs
                .Service<ICache, MemoryCache>(Lifetime.Singleton)
                .Instance<IConfig>(Config.Read("config.test.json"))
                .Factory<ILogger>(i => TraceLogger.Create<TestsBase>(), Lifetime.Singleton)
                .Provider<IDbConnection, MySqlDbConnectionProvider>(Lifetime.Scoped)
                .Service<IDbSchemaManager, SqlDbSchemaManager>(explicitArgs: new Dictionary<string, object?> { ["dbTag"] = null }, Lifetime.Scoped);
            OneTimeSetup(svcs);
        });

        public virtual void OneTimeSetup(IServiceCollection svcs) { }

        [OneTimeTearDown]
        public virtual void OneTimeTearDown() 
        {
            ScopeFactory?.Dispose();
            ScopeFactory = null;
        }

        [SetUp]
        public virtual void Setup()
        {
            Injector = ScopeFactory!.CreateScope();
            Injector.Get<ICache>().Clear();
        }

        [TearDown]
        public virtual void TearDown()
        {
            Injector?.Dispose();
            Injector = null;
        }
    }
}
