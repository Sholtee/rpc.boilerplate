using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using Moq;
using NUnit.Framework;

namespace Services.Tests
{
    using API;
    using DAL.API;

    [TestFixture]
    public class InstallerTests
    {
        [Test]
        public void Run_ShouldInitializeTheDatabaseAndRegisterTheRootUser() 
        {
            var mockSchemaManager = new Mock<IDbSchemaManager>(MockBehavior.Strict);
            mockSchemaManager
                .Setup(sm => sm.CreateTables(It.Is<Assembly[]>(asms => asms.Length > 0 && asms.All(asm => asm.FullName.Contains(".DAL.")))));

            var mockUserRepo = new Mock<IUserRepository>(MockBehavior.Strict);
            mockUserRepo
                .Setup(r => r.Create(It.Is<DAL.API.User>(u => u.FullName == "Root" && u.EmailOrUserName == "root@root.hu"), "cica12", It.Is<string[]>(grps => grps.Single() == "Admins"), default))
                .Returns(Task.FromResult(Guid.NewGuid()));

            typeof(DAL.User).GetHashCode(); // force to load the containing assembly

            var installer = new Installer(mockSchemaManager.Object, new string[] { "-u", "root@root.hu",  "-p", "cica12" }, mockUserRepo.Object);
            installer.Run(GetType().Assembly);

            mockSchemaManager.Verify(sm => sm.CreateTables(It.IsAny<Assembly[]>()), Times.Once);
            mockUserRepo.Verify(um => um.Create(It.IsAny<DAL.API.User>(), It.IsAny<string>(), It.IsAny<string[]>(), default));
        }
    }
}
