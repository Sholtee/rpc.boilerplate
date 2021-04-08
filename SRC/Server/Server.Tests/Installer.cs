﻿using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using Moq;
using NUnit.Framework;

namespace Server.Tests
{
    using Modules.API;
    using Services.API;

    [TestFixture]
    public class InstallerTests
    {
        [Test]
        public void Run_ShouldInitializeTheDatabaseAndRegisterTheRootUser() 
        {
            var mockSchemaManager = new Mock<IDbSchemaManager>(MockBehavior.Strict);
            mockSchemaManager
                .Setup(sm => sm.CreateTables(It.Is<Assembly[]>(asms => asms.Length > 0 && asms.All(asm => asm.FullName.Contains(".DAL.")))));

            var mockUserManager = new Mock<IUserManager>(MockBehavior.Strict);
            mockUserManager
                .Setup(um => um.Create(It.Is<User>(u => u.FullName == "Root" && u.EmailOrUserName == "root@root.hu"), "cica12", It.Is<string[]>(grps => grps.Single() == "Admins")))
                .Returns(Task.FromResult(Guid.NewGuid()));

            var installer = new Installer(mockSchemaManager.Object, new string[] { "-u", "root@root.hu",  "-p", "cica12" }, mockUserManager.Object);
            installer.Run();

            mockSchemaManager.Verify(sm => sm.CreateTables(It.IsAny<Assembly[]>()), Times.Once);
            mockUserManager.Verify(um => um.Create(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<string[]>()));
        }
    }
}