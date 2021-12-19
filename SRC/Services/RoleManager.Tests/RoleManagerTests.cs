using System;
using System.Security.Authentication;
using System.Threading.Tasks;

using Moq;
using NUnit.Framework;

using Tests.Base;

namespace Services.Tests
{
    using API;
    using DAL.API;

    [TestFixture]
    public class RoleManagerTests: Debuggable
    {
        [Test]
        public void GetAssignedRoles_ShouldHandleMissingSessionId() 
        {
            RoleManager roleManager = new(new Lazy<IUserRepository>(() => new Mock<IUserRepository>(MockBehavior.Strict).Object), new Lazy<ISessionRepository>(() => new Mock<ISessionRepository>().Object));
            Assert.That(roleManager.GetAssignedRoles(null), Is.EqualTo(Roles.AnonymousUser));
        }

        [Test]
        public void GetAssignedRoles_ShouldThrowOnInvalidSessionId()
        {
            Guid invalid = Guid.NewGuid();

            var mockRepo = new Mock<ISessionRepository>(MockBehavior.Strict);
            mockRepo
                .Setup(r => r.GetUserId(invalid, default))
                .Returns(Task.FromException<Guid>(new InvalidOperationException()));

            var roleManager = new RoleManager(new Lazy<IUserRepository>(() => new Mock<IUserRepository>(MockBehavior.Strict).Object), new Lazy<ISessionRepository>(() => mockRepo.Object));
            Assert.Throws<InvalidOperationException>(() => roleManager.GetAssignedRoles(invalid.ToString()));

            mockRepo.Verify(r => r.GetUserId(invalid, default), Times.Once);
        }

        [Test]
        public void GetAssignedRoles_ShouldHandleValidSessionId()
        {
            Guid
                user = Guid.NewGuid(),
                session = Guid.NewGuid();

            var mockSessionRepo = new Mock<ISessionRepository>(MockBehavior.Strict);
            mockSessionRepo
                .Setup(r => r.GetUserId(session, default))
                .Returns(Task.FromResult(user));

            var mockUserRepo = new Mock<IUserRepository>(MockBehavior.Strict);
            mockUserRepo
                .Setup(r => r.GetById(user, default))
                .Returns(Task.FromResult(new DAL.API.UserEx { Roles = Roles.Admin }));

            var roleManager = new RoleManager(new Lazy<IUserRepository>(() => mockUserRepo.Object), new Lazy<ISessionRepository>(() => mockSessionRepo.Object));
            Assert.That(roleManager.GetAssignedRoles(session.ToString()), Is.EqualTo(Roles.Admin | Roles.AuthenticatedUser));

            mockSessionRepo.Verify(r => r.GetUserId(session, default), Times.Once);
            mockUserRepo.Verify(r => r.GetById(user, default), Times.Once);
        }
    }
}
