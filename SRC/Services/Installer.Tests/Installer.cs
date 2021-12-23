using System;
using System.IO;
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
        public void Install_ShouldInitializeTheDatabaseAndRegisterTheRootUser()
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

            var mockConfig = new Mock<IConfig<DatabaseConfig>>(MockBehavior.Strict);
            mockConfig
                .SetupGet(c => c.Value)
                .Returns(value: null);

            typeof(DAL.User).GetHashCode(); // force loading the containing assembly

            Installer installer = new(mockSchemaManager.Object, mockUserRepo.Object, mockConfig.Object);

            Assert.DoesNotThrow(() => installer.Install(new InstallArguments 
            {
                User = "root@root.hu",
                Password = "cica12"
            }));
            
            mockSchemaManager.Verify(sm => sm.Initialize(), Times.Once);
            mockUserRepo.Verify(um => um.Create(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<string[]>(), default), Times.Once);
        }

        [Test]
        public void Status_ShouldPrintTheCurrentState()
        {
            var mockSchemaManager = new Mock<IDbSchemaManager>(MockBehavior.Strict);
            mockSchemaManager
                .SetupGet(sm => sm.IsInitialized)
                .Returns(true);

            DateTime lastMigration = DateTime.Now;

            mockSchemaManager
                .Setup(sm => sm.GetLastMigrationUtc())
                .Returns(lastMigration);

            var mockConfig = new Mock<IConfig<DatabaseConfig>>(MockBehavior.Strict);
            mockConfig
                .SetupGet(c => c.Value)
                .Returns(value: null);

            Installer installer = new(mockSchemaManager.Object, new Mock<IUserRepository>(MockBehavior.Strict).Object, mockConfig.Object);
            Assert.That(installer.Status, Is.EqualTo($"Installed (Last Migration: {lastMigration})"));
        }

        [Test]
        public void Status_ShouldSignalIfTheAppHasNotBeenInstalled()
        {
            var mockSchemaManager = new Mock<IDbSchemaManager>(MockBehavior.Strict);
            mockSchemaManager
                .SetupGet(sm => sm.IsInitialized)
                .Returns(false);

            var mockConfig = new Mock<IConfig<DatabaseConfig>>(MockBehavior.Strict);
            mockConfig
                .SetupGet(c => c.Value)
                .Returns(value: null);

            Installer installer = new(mockSchemaManager.Object, new Mock<IUserRepository>(MockBehavior.Strict).Object, mockConfig.Object);
            Assert.That(installer.Status, Is.EqualTo("Not installed"));
        }

        [Test]
        public void Migrate_ShouldReadTheMigrationScripts()
        {
            var mockSchemaManager = new Mock<IDbSchemaManager>(MockBehavior.Strict);
            mockSchemaManager
                .Setup(sm => sm.Migrate(File.GetCreationTimeUtc("Migration\\migration_script_1.sql"), string.Empty, "migration_script_1.sql"))
                .Returns(true);

            var mockConfig = new Mock<IConfig<DatabaseConfig>>(MockBehavior.Strict);
            mockConfig
                .SetupGet(c => c.Value)
                .Returns(new DatabaseConfig { MigrationDir = "Migration" });

            Installer installer = new(mockSchemaManager.Object, new Mock<IUserRepository>(MockBehavior.Strict).Object, mockConfig.Object);

            string[] scripts = installer.Migrate().ToArray();
            Assert.That(scripts.Length, Is.EqualTo(1));
            Assert.That(scripts[0], Is.EqualTo("Migration\\migration_script_1.sql [Installed]"));
        }
    }
}
