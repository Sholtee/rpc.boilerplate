using System;
using System.Linq;
using System.Threading.Tasks;

using Moq;
using NUnit.Framework;

using Tests.Base;

namespace Services.Tests
{
    using API;
    using DAL.API;

    [TestFixture]
    public class InstallerTests: Debuggable
    {
        [Test]
        public void Run_ShouldInitializeTheDatabaseAndRegisterTheRootUser()
        {
            var mockSchemaManager = new Mock<IDbSchemaManager>(MockBehavior.Strict);
            mockSchemaManager
                .Setup(sm => sm.Initialize());
            mockSchemaManager
                .SetupGet(sm => sm.IsInitialized)
                .Returns(false);

            var mockUserRepo = new Mock<IUserRepository>(MockBehavior.Strict);
            mockUserRepo
                .Setup(r => r.Create(It.Is<DAL.API.User>(u => u.FullName == "Superuser" && u.EmailOrUserName == "root@root.hu"), "cica12", It.Is<string[]>(grps => grps.Single() == "Admins"), default))
                .Returns(Task.FromResult(Guid.NewGuid()));

            typeof(DAL.User).GetHashCode(); // force loading the containing assembly

            Installer installer = new(mockSchemaManager.Object, mockUserRepo.Object, new Mock<IConfig>(MockBehavior.Strict).Object);

            Assert.DoesNotThrow(() => installer.Install(new InstallArguments 
            {
                User = "root@root.hu",
                Password = "cica12"
            }));
            
            mockSchemaManager.Verify(sm => sm.Initialize(), Times.Once);
            mockUserRepo.Verify(um => um.Create(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<string[]>(), default), Times.Once);
        }

        [Test]
        public void Run_ShouldPrintTheCurrentState()
        {
            var mockSchemaManager = new Mock<IDbSchemaManager>(MockBehavior.Strict);
            mockSchemaManager
                .SetupGet(sm => sm.IsInitialized)
                .Returns(true);

            DateTime lastMigration = DateTime.Now;

            mockSchemaManager
                .Setup(sm => sm.GetLastMigrationUtc())
                .Returns(lastMigration);

            Installer installer = new(mockSchemaManager.Object, new Mock<IUserRepository>(MockBehavior.Strict).Object, new Mock<IConfig>(MockBehavior.Strict).Object);
            Assert.That(installer.Status, Is.EqualTo($"INSTALLED (Last Migration: {lastMigration})"));
        }
    }
}
