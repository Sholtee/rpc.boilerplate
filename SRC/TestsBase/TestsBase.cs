using System.Data;

using Microsoft.Extensions.Logging;
using NUnit.Framework;

using ServiceStack.Data;

using Solti.Utils.DI.Interfaces;

namespace Tests.Base
{
    using Server.Loggers;
    using Services.API;
    using Services;

    public class TestsBase
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
                .Provider<IDbConnectionFactory, MySqlDbConnectionFactoryProvider>(Lifetime.Singleton)
                .Provider<IDbConnection, SqlConnectionProvider>(Lifetime.Scoped)
                .Service<IDbSchemaManager, SqlDbSchemaManager>(Lifetime.Scoped);
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
