using System;
using System.Data;
using System.Security.Authentication;
using System.Threading.Tasks;

using NUnit.Framework;
using ServiceStack.OrmLite;

using Tests.Base;

using Solti.Utils.DI.Interfaces;

namespace DAL.Tests
{
    using API;
    using Services.API;

    [TestFixture]
    public class UserRepositoryTests : TestsBase
    {
        public IUserRepository UserRepository { get; set; }

        public IDbSchemaManager SchemaManager { get; set; }

        public IDbConnection Connection { get; set; }

        public override void OneTimeSetup()
        {
            base.OneTimeSetup();

            Container.Service<IUserRepository, SqlUserRepository>(Lifetime.Scoped);
        }

        public override void Setup()
        {
            base.Setup();

            SchemaManager = Injector.Get<IDbSchemaManager>();
            UserRepository = Injector.Get<IUserRepository>();
            Connection = Injector.Get<IDbConnection>();

            SchemaManager.CreateTables(typeof(DAL.User).Assembly);
        }

        public override void TearDown()
        {
            SchemaManager.DropTables(typeof(DAL.User).Assembly);

            base.TearDown();
        }

        [Test]
        public void Create_ShouldCreateTheAppropriateEntries() 
        {
            long id = 0;
            Assert.DoesNotThrowAsync(async() => id = await UserRepository.Create(new API.User { FullName = "cica1", EmailOrUserName = "abc@def.hu"}, "mica1"));
            Assert.That(id, Is.GreaterThan(0));

            DAL.User user = Connection.SingleById<DAL.User>(id);

            Assert.That(user.FullName, Is.EqualTo("cica1"));
            Assert.That(user.LoginId, Is.GreaterThan(0));

            DAL.Login login = Connection.SingleById<DAL.Login>(user.LoginId);

            Assert.That(login.EmailOrUserName, Is.EqualTo("abc@def.hu"));
        }

        [Test]
        public void Create_ShouldThrowOnExistingUser()
        {
            Assert.DoesNotThrowAsync(() => UserRepository.Create(new API.User { FullName = "cica1", EmailOrUserName = "abc@def.hu" }, "mica1"));
            Assert.ThrowsAsync<InvalidOperationException>(() => UserRepository.Create(new API.User { FullName = "cica1", EmailOrUserName = "abc@def.hu" }, "xXx12")); // TODO: verify message
        }

        [Test]
        public async Task QueryByCredentials_ShouldReturnTheProperEntry() 
        {
            await UserRepository.Create(new API.User { FullName = "cica1", EmailOrUserName = "abc@def.hu" }, "mica1");

            API.User user = null;
            Assert.DoesNotThrowAsync(async () => user = await UserRepository.QueryByCredentials("abc@def.hu", "mica1"));

            Assert.That(user, Is.Not.Null);
            Assert.That(user.EmailOrUserName, Is.EqualTo("abc@def.hu"));
            Assert.That(user.FullName, Is.EqualTo("cica1"));
        }

        [Test]
        public async Task QueryByCredentials_ShouldThrowOnInvalidCredentials()
        {
            await UserRepository.Create(new API.User { FullName = "cica1", EmailOrUserName = "abc@def.hu" }, "mica1");

            Assert.ThrowsAsync<InvalidCredentialException>(() => UserRepository.QueryByCredentials("abc@def.hu", "kutya12"));
            Assert.ThrowsAsync<InvalidCredentialException>(() => UserRepository.QueryByCredentials("abc@def.hu_", "mica1"));
        }

        [Test]
        public async Task QueryById_ShouldReturnTheProperEntry()
        {
            long id = await UserRepository.Create(new API.User { FullName = "cica1", EmailOrUserName = "abc@def.hu" }, "mica1");

            API.User user = null;            
            Assert.DoesNotThrowAsync(async () => user = await UserRepository.QueryById(id));

            Assert.That(user, Is.Not.Null);
            Assert.That(user.EmailOrUserName, Is.EqualTo("abc@def.hu"));
            Assert.That(user.FullName, Is.EqualTo("cica1"));
        }

        [Test]
        public async Task Delete_ShouldNotDeletePhysically()
        {
            long id = await UserRepository.Create(new API.User { FullName = "cica1", EmailOrUserName = "abc@def.hu" }, "mica1");

            Assert.DoesNotThrow(() => UserRepository.Delete(id));
            Assert.ThrowsAsync<InvalidOperationException>(() => UserRepository.QueryById(id));

            DAL.User user = Connection.SingleById<DAL.User>(id);
            DAL.Login login = Connection.SingleById<DAL.Login>(user.LoginId);

            Assert.That(login.Deleted, Is.Not.Null);
        }

        [Test]
        public void Delete_ShouldThrowOnInvalidId() => Assert.ThrowsAsync<InvalidOperationException>(() => UserRepository.Delete(0));
    }
}
