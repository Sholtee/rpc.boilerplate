using System;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;

using Moq;
using NUnit.Framework;

using Solti.Utils.Rpc.Interfaces;

namespace Modules.Tests
{
    using DAL.API;
    using Services.API;

    [TestFixture]
    public class UserManagerTests
    {
        [Test]
        public void Create_ShouldFileTheNewUser() 
        {
            var mockRepo = new Mock<IUserRepository>(MockBehavior.Strict);
            mockRepo
                .Setup(r => r.Create(It.Is<DAL.API.User>(u => u.EmailOrUserName == "cica@mica.hu" && u.FullName == "cica"), "kutya", default))
                .Returns(Task.FromResult(Guid.NewGuid()));

            var mockContext = new Mock<IRequestContext>(MockBehavior.Strict);
            mockContext
                .SetupGet(c => c.Cancellation)
                .Returns(default(CancellationToken));

            var userManager = new UserManager(new Lazy<IUserRepository>(() => mockRepo.Object), new Lazy<ICache>(() => new Mock<ICache>(MockBehavior.Strict).Object), new Lazy<IConfig>(() => new Mock<IConfig>(MockBehavior.Strict).Object), mockContext.Object);

            Assert.DoesNotThrowAsync(() => userManager.Create(new API.User { FullName = "cica", EmailOrUserName = "cica@mica.hu" }, "kutya"));
            mockRepo.Verify(r => r.Create(It.IsAny<DAL.API.User>(), "kutya", default), Times.Once);
            mockContext.VerifyGet(c => c.Cancellation, Times.Once);
        }

        [Test]
        public void Login_ShouldThrowOnInvalidCredential() 
        {
            var mockRepo = new Mock<IUserRepository>(MockBehavior.Strict);
            mockRepo
                .Setup(r => r.QueryByCredentials(It.IsAny<string>(), It.IsAny<string>(), default))
                .ThrowsAsync(new InvalidCredentialException());

            var mockContext = new Mock<IRequestContext>(MockBehavior.Strict);
            mockContext
                .SetupGet(c => c.Cancellation)
                .Returns(default(CancellationToken));

            var userManager = new UserManager(new Lazy<IUserRepository>(() => mockRepo.Object), new Lazy<ICache>(() => new Mock<ICache>(MockBehavior.Strict).Object), new Lazy<IConfig>(() => new Mock<IConfig>(MockBehavior.Strict).Object), mockContext.Object);

            Assert.ThrowsAsync<InvalidCredentialException>(() => userManager.Login("cica", "kutya"));
            mockContext.VerifyGet(c => c.Cancellation, Times.Once);
        }

        [Test]
        public void Login_ShouldCreateANewSession()
        {
            var user = new DAL.API.User
            {
                Id = Guid.NewGuid(),
                EmailOrUserName = "cica"
            };

            var mockRepo = new Mock<IUserRepository>(MockBehavior.Strict);
            mockRepo
                .Setup(r => r.QueryByCredentials("cica", "kutya", default))
                .Returns(Task.FromResult(user));

            var mockCache = new Mock<ICache>(MockBehavior.Strict);
            mockCache
                .Setup(c => c.Add(It.Is<string>(s => IsGuid(s)), user, TimeSpan.FromMinutes(10)))
                .Returns(true);

            var mockConfig = new Mock<IConfig>(MockBehavior.Strict);
            mockConfig
                .SetupGet(c => c.Server)
                .Returns(new ServerConfig { SessionTimeoutInMinutes = 10 });

            var mockContext = new Mock<IRequestContext>(MockBehavior.Strict);
            mockContext
                .SetupGet(c => c.Cancellation)
                .Returns(default(CancellationToken));

            var userManager = new UserManager(
                new Lazy<IUserRepository>(() => mockRepo.Object), 
                new Lazy<ICache>(() => mockCache.Object), 
                new Lazy<IConfig>(() => mockConfig.Object), 
                mockContext.Object);

            Guid sessionId = Guid.Empty;
            Assert.DoesNotThrowAsync(async () => sessionId = await userManager.Login("cica", "kutya"));
            Assert.That(sessionId, Is.Not.EqualTo(Guid.Empty));

            mockRepo.Verify(r => r.QueryByCredentials("cica", "kutya", default), Times.Once);
            mockCache.Verify(c => c.Add(It.Is<string>(s => IsGuid(s)), user, TimeSpan.FromMinutes(10)), Times.Once);
            mockConfig.VerifyGet(c => c.Server, Times.Once);
            mockContext.VerifyGet(c => c.Cancellation, Times.Once);
        }

        private static bool IsGuid(string s) => Guid.TryParse(s, out Guid _);

        [Test]
        public void Logout_ShouldInvalidateTheSession() 
        {
            var mockContext = new Mock<IRequestContext>(MockBehavior.Strict);
            mockContext
                .Setup(ctx => ctx.SessionId)
                .Returns("cica");

            var mockCache = new Mock<ICache>(MockBehavior.Strict);
            mockCache
                .Setup(c => c.Remove("cica"))
                .Returns(true);

            var userManager = new UserManager(
                new Lazy<IUserRepository>(() => new Mock<IUserRepository>(MockBehavior.Strict).Object),
                new Lazy<ICache>(() => mockCache.Object),
                new Lazy<IConfig>(() => new Mock<IConfig>(MockBehavior.Strict).Object),
                mockContext.Object);

            Assert.DoesNotThrow(userManager.Logout);
            mockCache.Verify(c => c.Remove("cica"), Times.Once);
        }
    }
}
