using System.Security.Authentication;

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
            var mockCache = new Mock<ICache>(MockBehavior.Strict);

            var roleManager = new RoleManager(mockCache.Object);
            Assert.That(roleManager.GetAssignedRoles(null), Is.EqualTo(Roles.AnonymousUser));
        }

        [Test]
        public void GetAssignedRoles_ShouldThrowOnInvalidSessionId()
        {
            User user;

            var mockCache = new Mock<ICache>(MockBehavior.Strict);
            mockCache
                .Setup(c => c.TryGetValue("invalid", out user))
                .Returns(false);

            var roleManager = new RoleManager(mockCache.Object);
            Assert.Throws<InvalidCredentialException>(() => roleManager.GetAssignedRoles("invalid"));

            mockCache.Verify(c => c.TryGetValue("invalid", out user), Times.Once);
        }

        [Test]
        public void GetAssignedRoles_ShouldHandleValidSessionId()
        {
            User user;

            var mockCache = new Mock<ICache>(MockBehavior.Strict);
            mockCache
                .Setup(c => c.TryGetValue("valid", out user))
                .Returns(true);

            var roleManager = new RoleManager(mockCache.Object);
            Assert.That(roleManager.GetAssignedRoles("valid"), Is.EqualTo(Roles.AuthenticatedUser));

            mockCache.Verify(c => c.TryGetValue("valid", out user), Times.Once);
        }
    }
}
