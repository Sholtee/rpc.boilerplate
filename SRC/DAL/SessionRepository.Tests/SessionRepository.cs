using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

using NUnit.Framework;

using Tests.Base;

using Solti.Utils.DI.Interfaces;

namespace DAL.Tests
{
    using API;
    using DAL;
    using Services;
    using Services.API;

    [TestFixture]
    public class SessionRepositoryTests : TestsBase
    {
        public ISessionRepository SessionRepository { get; set; }

        public IDbSchemaManager SchemaManager { get; set; }

        public override void OneTimeSetup(IServiceCollection svcs) => svcs
            .Provider<IDbConnection, SQLiteDbConnectionProvider>(SQLiteDbConnectionProvider.ServiceName, Lifetime.Scoped)
            .Service<ISessionRepository, SqlSessionRepository>(Lifetime.Scoped)
            .Service<IDbSchemaManager, SqlDbSchemaManager>(SQLiteDbConnectionProvider.ServiceName, explicitArgs: new Dictionary<string, object> { ["dbTag"] = SQLiteDbConnectionProvider.ServiceName }, Lifetime.Scoped);

        public override void Setup()
        {
            base.Setup();

            SchemaManager = Injector.Get<IDbSchemaManager>(SQLiteDbConnectionProvider.ServiceName);
            SessionRepository = Injector.Get<ISessionRepository>();

            SchemaManager.Initialize();
        }

        public override void TearDown()
        {
            SchemaManager.Drop();

            base.TearDown();
        }

        [Test]
        public async Task GetOrCreate_ShouldCreateANewSession() =>
            Assert.That(await SessionRepository.GetOrCreate(Guid.NewGuid()), Is.Not.EqualTo(Guid.Empty));

        [Test]
        public async Task GetOrCreate_ShouldCreateANewSessionIfThePreviousExpired()
        {
            Guid
                userId = Guid.NewGuid(),
                sessionId = await SessionRepository.GetOrCreate(userId);
            
            await SessionRepository.ExpireById(sessionId);

            Assert.That(await SessionRepository.GetOrCreate(userId), Is.Not.EqualTo(sessionId));
        }

        [Test]
        public async Task GetOrCreate_ShouldReturnTheLivingSession()
        {
            Guid userId = Guid.NewGuid();

            Assert.AreEqual(await SessionRepository.GetOrCreate(userId), await SessionRepository.GetOrCreate(userId));
        }
    }
}
