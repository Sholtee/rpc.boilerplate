using System.Data;

using NUnit.Framework;

using ServiceStack.Data;

using Solti.Utils.DI;
using Solti.Utils.DI.Interfaces;

namespace Tests.Base
{
    using Services.API;
    using Services;

    public class TestsBase
    {
        public IServiceContainer? Container { get; private set; }

        public IInjector? Injector { get; private set; }

        [OneTimeSetUp]
        public virtual void OneTimeSetup() 
        {
            Container = new ServiceContainer();

            Container
                .Instance<IConfig>(Config.Read("config.test.json"))
                .Provider<IDbConnectionFactory, MySqlDbConnectionFactoryProvider>(Lifetime.Singleton)
                .Provider<IDbConnection, SqlConnectionProvider>(Lifetime.Scoped)
                //.Service<ICache, RedisCache>(Lifetime.Scoped)
                .Service<ICache, MemoryCache>(Lifetime.Singleton)
                .Service<IDbSchemaManager, SqlDbSchemaManager>(Lifetime.Scoped);
        }

        [OneTimeTearDown]
        public virtual void OneTimeTearDown() 
        {
            Container?.Dispose();
            Container = null;
        }

        [SetUp]
        public virtual void Setup()
        {
            Injector = Container!.CreateInjector();
        }

        [TearDown]
        public virtual void TearDown()
        {
            Injector?.Dispose();
            Injector = null;
        }
    }
}
