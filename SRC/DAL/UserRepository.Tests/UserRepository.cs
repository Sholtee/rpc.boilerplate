using System;
using System.Collections.Generic;
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
    using DAL;
    using Services;
    using Services.API;

    [TestFixture]
    public class UserRepositoryTests : TestsBase
    {
        public IUserRepository UserRepository { get; set; }

        public IDbSchemaManager SchemaManager { get; set; }

        public IDbConnection Connection { get; set; }

        public override void OneTimeSetup(IServiceCollection svcs) => svcs
            .Provider<IDbConnection, MySqlDbConnectionProvider>(Lifetime.Scoped)
            .Service<IUserRepository, SqlUserRepository>(Lifetime.Scoped)
            .Service<IDbSchemaManager, SqlDbSchemaManager>(explicitArgs: new Dictionary<string, object> { ["dbTag"] = null }, Lifetime.Scoped);

        public override void Setup()
        {
            base.Setup();

            SchemaManager = Injector.Get<IDbSchemaManager>();
            UserRepository = Injector.Get<IUserRepository>();
            Connection = Injector.Get<IDbConnection>();

            SchemaManager.Initialize();
        }

        public override void TearDown()
        {
            SchemaManager.Drop();

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
            Assert.That(user.EmailOrUserName, Is.EqualTo("abc@def.hu"));
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
        public async Task GetByCredentials_ShouldReturnTheProperEntry()
        {
            await UserRepository.Create(new API.User { FullName = "cica1", EmailOrUserName = "abc@def.hu" }, "mica1", Array.Empty<string>());

            API.User user = null;
            Assert.DoesNotThrowAsync(async () => user = await UserRepository.GetByCredentials("abc@def.hu", "mica1"));

            Assert.That(user, Is.Not.Null);
            Assert.That(user.EmailOrUserName, Is.EqualTo("abc@def.hu"));
            Assert.That(user.FullName, Is.EqualTo("cica1"));
        }

        [Test]
        public async Task GetByCredentials_ShouldThrowOnInvalidCredentials()
        {
            await UserRepository.Create(new API.User { FullName = "cica1", EmailOrUserName = "abc@def.hu" }, "mica1", Array.Empty<string>());

            Assert.ThrowsAsync<InvalidCredentialException>(() => UserRepository.GetByCredentials("abc@def.hu", "kutya12"));
            Assert.ThrowsAsync<InvalidCredentialException>(() => UserRepository.GetByCredentials("abc@def.hu_", "mica1"));
        }

        [Test]
        public async Task GetByCredentials_ShouldReturnAllTheAssignedRoles()
        {
            Connection.Insert(new DAL.Group 
            {
                Name = "StandardUsers",
                Roles = Roles.AuthenticatedUser
            });

            await UserRepository.Create(new API.User { FullName = "cica1", EmailOrUserName = "abc@def.hu" }, "mica1", new[] { "StandardUsers" });
            await UserRepository.Create(new API.User { FullName = "cica2", EmailOrUserName = "xyz@abc.hu" }, "mica2", new[] { "Admins", "StandardUsers" });

            API.UserEx user = await UserRepository.GetByCredentials("abc@def.hu", "mica1");
            Assert.That(user.Roles, Is.EqualTo(Roles.AuthenticatedUser));

            user = await UserRepository.GetByCredentials("xyz@abc.hu", "mica2");
            Assert.That(user.Roles, Is.EqualTo(Roles.AuthenticatedUser | Roles.Admin));
        }

        [Test]
        public async Task GetById_ShouldReturnTheProperEntry()
        {
            Guid id = await UserRepository.Create(new API.User { FullName = "cica1", EmailOrUserName = "abc@def.hu" }, "mica1", Array.Empty<string>());

            API.User user = null;
            Assert.DoesNotThrowAsync(async () => user = await UserRepository.GetById(id));

            Assert.That(user, Is.Not.Null);
            Assert.That(user.EmailOrUserName, Is.EqualTo("abc@def.hu"));
            Assert.That(user.FullName, Is.EqualTo("cica1"));
        }

        [Test]
        public async Task GetById_ShouldReturnAllTheAssignedRoles()
        {
            Connection.Insert(new DAL.Group
            {
                Name = "StandardUsers",
                Roles = Roles.AuthenticatedUser
            });

            Guid
                id1 = await UserRepository.Create(new API.User { FullName = "cica1", EmailOrUserName = "abc@def.hu" }, "mica1", new[] { "StandardUsers" }),
                id2 = await UserRepository.Create(new API.User { FullName = "cica2", EmailOrUserName = "xyz@abc.hu" }, "mica2", new[] { "Admins", "StandardUsers" });

            API.UserEx user = await UserRepository.GetById(id1);
            Assert.That(user.Roles, Is.EqualTo(Roles.AuthenticatedUser));

            user = await UserRepository.GetById(id2);
            Assert.That(user.Roles, Is.EqualTo(Roles.AuthenticatedUser | Roles.Admin));
        }

        [Test]
        public async Task Delete_ShouldNotDeletePhysically()
        {
            Guid id = await UserRepository.Create(new API.User { FullName = "cica1", EmailOrUserName = "abc@def.hu" }, "mica1", Array.Empty<string>());

            Assert.DoesNotThrowAsync(() => UserRepository.DeleteById(id));
            Assert.ThrowsAsync<InvalidOperationException>(() => UserRepository.GetById(id));

            DAL.User user = Connection.SingleById<DAL.User>(id);

            Assert.That(user.DeletedUtc, Is.Not.Null);
        }

        [Test]
        public void Delete_ShouldThrowOnInvalidId() => Assert.ThrowsAsync<InvalidOperationException>(() => UserRepository.DeleteById(Guid.NewGuid()));

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

            await UserRepository.DeleteById(id);

            PartialUserList lst = await UserRepository.List(0, int.MaxValue);
            Assert.That(lst.AllEntries, Is.EqualTo(1));
            Assert.That(lst.Entries.Single().EmailOrUserName, Is.EqualTo("abc@def.hu"));
        }
    }
}
