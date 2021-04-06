using System;
using System.Data;
using System.Linq;
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
            Guid id = Guid.Empty;
            Assert.DoesNotThrowAsync(async () => id = await UserRepository.Create(new API.User { FullName = "cica1", EmailOrUserName = "abc@def.hu" }, "mica1", Array.Empty<string>()));
            Assert.That(id, Is.Not.EqualTo(Guid.Empty));

            DAL.User user = Connection.SingleById<DAL.User>(id);

            Assert.That(user.FullName, Is.EqualTo("cica1"));
            Assert.That(user.LoginId, Is.Not.EqualTo(Guid.Empty));

            DAL.Login login = Connection.SingleById<DAL.Login>(user.LoginId);

            Assert.That(login.EmailOrUserName, Is.EqualTo("abc@def.hu"));
        }

        [Test]
        public void Create_ShouldThrowOnExistingUser()
        {
            Assert.DoesNotThrowAsync(() => UserRepository.Create(new API.User { FullName = "cica1", EmailOrUserName = "abc@def.hu" }, "mica1", Array.Empty<string>()));
            Assert.ThrowsAsync<InvalidOperationException>(() => UserRepository.Create(new API.User { FullName = "cica1", EmailOrUserName = "abc@def.hu" }, "xXx12", Array.Empty<string>())); // TODO: verify message
        }

        [Test]
        public void Create_ShouldThrowOnInvalidGroup() 
        {
            Assert.ThrowsAsync<InvalidOperationException>(() => UserRepository.Create(new API.User { FullName = "cica1", EmailOrUserName = "abc@def.hu" }, "xXx12", new[] { "invalid"} ));
        }

        [Test]
        public async Task QueryByCredentials_ShouldReturnTheProperEntry()
        {
            await UserRepository.Create(new API.User { FullName = "cica1", EmailOrUserName = "abc@def.hu" }, "mica1", Array.Empty<string>());

            API.User user = null;
            Assert.DoesNotThrowAsync(async () => user = await UserRepository.QueryByCredentials("abc@def.hu", "mica1"));

            Assert.That(user, Is.Not.Null);
            Assert.That(user.EmailOrUserName, Is.EqualTo("abc@def.hu"));
            Assert.That(user.FullName, Is.EqualTo("cica1"));
        }

        [Test]
        public async Task QueryByCredentials_ShouldThrowOnInvalidCredentials()
        {
            await UserRepository.Create(new API.User { FullName = "cica1", EmailOrUserName = "abc@def.hu" }, "mica1", Array.Empty<string>());

            Assert.ThrowsAsync<InvalidCredentialException>(() => UserRepository.QueryByCredentials("abc@def.hu", "kutya12"));
            Assert.ThrowsAsync<InvalidCredentialException>(() => UserRepository.QueryByCredentials("abc@def.hu_", "mica1"));
        }

        [Test]
        public async Task QueryByCredentials_ShouldReturnAllTheAssignedRoles()
        {
            Connection.Insert(new DAL.Group
            {
                Name = "Admins",
                Roles = Roles.Admin
            });
            Connection.Insert(new DAL.Group 
            {
                Name = "StandardUsers",
                Roles = Roles.AuthenticatedUser
            });

            await UserRepository.Create(new API.User { FullName = "cica1", EmailOrUserName = "abc@def.hu" }, "mica1", new[] { "StandardUsers" });
            await UserRepository.Create(new API.User { FullName = "cica2", EmailOrUserName = "xyz@abc.hu" }, "mica2", new[] { "Admins", "StandardUsers" });

            API.UserEx user = await UserRepository.QueryByCredentials("abc@def.hu", "mica1");
            Assert.That(user.Roles, Is.EqualTo(Roles.AuthenticatedUser));

            user = await UserRepository.QueryByCredentials("xyz@abc.hu", "mica2");
            Assert.That(user.Roles, Is.EqualTo(Roles.AuthenticatedUser | Roles.Admin));
        }

        [Test]
        public async Task QueryBySession_ShouldReturnTheProperEntry()
        {
            Guid id = await UserRepository.Create(new API.User { FullName = "cica1", EmailOrUserName = "abc@def.hu" }, "mica1", Array.Empty<string>());
            Guid sessionId = await UserRepository.CreateSession(id);

            API.User user = null;
            Assert.DoesNotThrowAsync(async () => user = await UserRepository.QueryBySession(sessionId));

            Assert.That(user, Is.Not.Null);
            Assert.That(user.EmailOrUserName, Is.EqualTo("abc@def.hu"));
            Assert.That(user.FullName, Is.EqualTo("cica1"));
        }


        [Test]
        public async Task QueryBySession_ShouldReturnAllTheAssignedRoles()
        {
            Connection.Insert(new DAL.Group
            {
                Name = "Admins",
                Roles = Roles.Admin
            });
            Connection.Insert(new DAL.Group
            {
                Name = "StandardUsers",
                Roles = Roles.AuthenticatedUser
            });

            Guid 
                sessionId1 = await UserRepository.CreateSession(await UserRepository.Create(new API.User { FullName = "cica1", EmailOrUserName = "abc@def.hu" }, "mica1", new[] { "StandardUsers" })),
                sessionId2 = await UserRepository.CreateSession(await UserRepository.Create(new API.User { FullName = "cica2", EmailOrUserName = "xyz@abc.hu" }, "mica2", new[] { "Admins", "StandardUsers" }));

            API.UserEx user = await UserRepository.QueryBySession(sessionId1);
            Assert.That(user.Roles, Is.EqualTo(Roles.AuthenticatedUser));

            user = await UserRepository.QueryBySession(sessionId2);
            Assert.That(user.Roles, Is.EqualTo(Roles.AuthenticatedUser | Roles.Admin));
        }

        [Test]
        public async Task QueryBySession_ShouldThrowOnInvalidCredentials()
        {
            Guid id = await UserRepository.Create(new API.User { FullName = "cica1", EmailOrUserName = "abc@def.hu" }, "mica1", Array.Empty<string>());
            await UserRepository.CreateSession(id);

            Assert.ThrowsAsync<InvalidCredentialException>(() => UserRepository.QueryBySession(Guid.NewGuid()));
        }

        [Test]
        public async Task QueryById_ShouldReturnTheProperEntry()
        {
            Guid id = await UserRepository.Create(new API.User { FullName = "cica1", EmailOrUserName = "abc@def.hu" }, "mica1", Array.Empty<string>());

            API.User user = null;
            Assert.DoesNotThrowAsync(async () => user = await UserRepository.QueryById(id));

            Assert.That(user, Is.Not.Null);
            Assert.That(user.EmailOrUserName, Is.EqualTo("abc@def.hu"));
            Assert.That(user.FullName, Is.EqualTo("cica1"));
        }

        [Test]
        public async Task Delete_ShouldNotDeletePhysically()
        {
            Guid id = await UserRepository.Create(new API.User { FullName = "cica1", EmailOrUserName = "abc@def.hu" }, "mica1", Array.Empty<string>());

            Assert.DoesNotThrowAsync(() => UserRepository.Delete(id));
            Assert.ThrowsAsync<InvalidOperationException>(() => UserRepository.QueryById(id));

            DAL.User user = Connection.SingleById<DAL.User>(id);
            DAL.Login login = Connection.SingleById<DAL.Login>(user.LoginId);

            Assert.That(login.DeletedUtc, Is.Not.Null);
        }

        [Test]
        public async Task Delete_ShouldDeleteTheLivingSession() 
        {
            Guid 
                id = await UserRepository.Create(new API.User { FullName = "cica1", EmailOrUserName = "abc@def.hu" }, "mica1", Array.Empty<string>()),
                sessionId = await UserRepository.CreateSession(id);

            Assert.DoesNotThrowAsync(() => UserRepository.QueryBySession(sessionId));
            Assert.DoesNotThrowAsync(() => UserRepository.Delete(id));
            Assert.ThrowsAsync<InvalidCredentialException>(() => UserRepository.QueryBySession(sessionId));
        }

        [Test]
        public void Delete_ShouldThrowOnInvalidId() => Assert.ThrowsAsync<InvalidOperationException>(() => UserRepository.Delete(Guid.NewGuid()));

        [Test]
        public async Task List_ShouldPaging()
        {
            await UserRepository.Create(new API.User { FullName = "cica1", EmailOrUserName = "abc@def.hu" }, "mica1", Array.Empty<string>());
            await UserRepository.Create(new API.User { FullName = "kutya", EmailOrUserName = "def@def.hu" }, "kutya", Array.Empty<string>());

            PartialUserList lst = await UserRepository.List(0, 1);
            Assert.That(lst.AllEntries, Is.EqualTo(2));
            Assert.That(lst.Entries.Single().EmailOrUserName, Is.EqualTo("abc@def.hu"));

            lst = await UserRepository.List(1, 1);
            Assert.That(lst.AllEntries, Is.EqualTo(2));
            Assert.That(lst.Entries.Single().EmailOrUserName, Is.EqualTo("def@def.hu"));
        }

        [Test]
        public async Task List_ShouldNotReturnDeletedEntries()
        {
            await UserRepository.Create(new API.User { FullName = "cica1", EmailOrUserName = "abc@def.hu" }, "mica1", Array.Empty<string>());
            Guid id = await UserRepository.Create(new API.User { FullName = "kutya", EmailOrUserName = "def@def.hu" }, "kutya", Array.Empty<string>());

            await UserRepository.Delete(id);

            PartialUserList lst = await UserRepository.List(0, int.MaxValue);
            Assert.That(lst.AllEntries, Is.EqualTo(1));
            Assert.That(lst.Entries.Single().EmailOrUserName, Is.EqualTo("abc@def.hu"));
        }

        [Test]
        public async Task CreateSession_ShouldCreateANewSession() 
        {
            Guid 
                id = await UserRepository.Create(new API.User { FullName = "cica1", EmailOrUserName = "abc@def.hu" }, "mica1", Array.Empty<string>()),
                sessionId1 = await UserRepository.CreateSession(id);

            Assert.That(sessionId1, Is.Not.EqualTo(Guid.Empty));

            await UserRepository.DeleteSession(sessionId1);
            await Task.Delay(1000);

            Guid sessionId2 = await UserRepository.CreateSession(id);
            Assert.That(sessionId2, Is.Not.EqualTo(Guid.Empty));
            Assert.AreNotEqual(sessionId1, sessionId2);
        }

        [Test]
        public async Task CreateSession_ShouldReturnTheLivingSession() 
        {
            Guid 
                id = await UserRepository.Create(new API.User { FullName = "cica1", EmailOrUserName = "abc@def.hu" }, "mica1", Array.Empty<string>()),
                sessionId1 = await UserRepository.CreateSession(id);

            Assert.That(sessionId1, Is.Not.EqualTo(Guid.Empty));

            await UserRepository.DeleteSession(sessionId1);
            await Task.Delay(1000);

            Guid sessionId2 = await UserRepository.CreateSession(id);
            Assert.AreEqual(sessionId2, await UserRepository.CreateSession(id));
        }
    }
}
