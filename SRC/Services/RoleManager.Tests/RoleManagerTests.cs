using System;
using System.Security.Authentication;
using System.Threading.Tasks;

using Moq;
using NUnit.Framework;

namespace Services.Tests
{
    using API;
    using DAL.API;

    [TestFixture]
    public class RoleManagerTests
    {
        [Test]
        public void GetAssignedRoles_ShouldHandleMissingSessionId() 
        {
            var mockRepo = new Mock<IUserRepository>(MockBehavior.Strict);

            var roleManager = new RoleManager(new Lazy<IUserRepository>(() => mockRepo.Object));
            Assert.That(roleManager.GetAssignedRoles(null), Is.EqualTo(Roles.AnonymousUser));
        }

        [Test]
        public void GetAssignedRoles_ShouldThrowOnInvalidSessionId()
        {
            Guid invalid = Guid.NewGuid();

            var mockRepo = new Mock<IUserRepository>(MockBehavior.Strict);
            mockRepo
                .Setup(r => r.QueryBySession(invalid, default))
                .Returns(Task.FromException<DAL.API.UserEx>(new InvalidCredentialException()));

            var roleManager = new RoleManager(new Lazy<IUserRepository>(() => mockRepo.Object));
            Assert.Throws<InvalidCredentialException>(() => roleManager.GetAssignedRoles(invalid.ToString()));

            mockRepo.Verify(r => r.QueryBySession(invalid, default), Times.Once);
        }

        [Test]
        public void GetAssignedRoles_ShouldHandleValidSessionId()
        {
            Guid session = Guid.NewGuid();

            var mockRepo = new Mock<IUserRepository>(MockBehavior.Strict);
            mockRepo
                .Setup(r => r.QueryBySession(session, default))
                .Returns(Task.FromResult(new DAL.API.UserEx { Roles = Roles.Admin }));

            var roleManager = new RoleManager(new Lazy<IUserRepository>(() => mockRepo.Object));
            Assert.That(roleManager.GetAssignedRoles(session.ToString()), Is.EqualTo(Roles.Admin | Roles.AuthenticatedUser));

            mockRepo.Verify(r => r.QueryBySession(session, default), Times.Once);
        }
    }
}
