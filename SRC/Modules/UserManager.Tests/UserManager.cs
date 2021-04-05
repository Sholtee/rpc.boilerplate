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

            var userManager = new UserManager(new Lazy<IUserRepository>(() => mockRepo.Object), mockContext.Object);

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

            var userManager = new UserManager(new Lazy<IUserRepository>(() => mockRepo.Object), mockContext.Object);

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
            mockRepo
                .Setup(r => r.CreateSession(user.Id.Value, default))
                .Returns(() => Task.FromResult(Guid.NewGuid()));

            var mockContext = new Mock<IRequestContext>(MockBehavior.Strict);
            mockContext
                .SetupGet(c => c.Cancellation)
                .Returns(default(CancellationToken));

            var userManager = new UserManager(new Lazy<IUserRepository>(() => mockRepo.Object), mockContext.Object);

            Guid sessionId = Guid.Empty;
            Assert.DoesNotThrowAsync(async () => sessionId = await userManager.Login("cica", "kutya"));
            Assert.That(sessionId, Is.Not.EqualTo(Guid.Empty));

            mockRepo.Verify(r => r.QueryByCredentials("cica", "kutya", default), Times.Once);
            mockRepo.Verify(r => r.CreateSession(user.Id.Value, default), Times.Once);
            mockContext.VerifyGet(c => c.Cancellation, Times.AtLeastOnce);
        }

        [Test]
        public void Logout_ShouldInvalidateTheSession() 
        {
            Guid sessionId = Guid.NewGuid();

            var mockContext = new Mock<IRequestContext>(MockBehavior.Strict);
            mockContext
                .Setup(ctx => ctx.SessionId)
                .Returns(sessionId.ToString());
            mockContext
                .Setup(ctx => ctx.Cancellation)
                .Returns(() => default);

            var mockRepo = new Mock<IUserRepository>(MockBehavior.Strict);
            mockRepo
                .Setup(r => r.DeleteSession(sessionId, default))
                .Returns(Task.CompletedTask);

            var userManager = new UserManager(new Lazy<IUserRepository>(() => mockRepo.Object), mockContext.Object);

            Assert.DoesNotThrowAsync(() => userManager.Logout());
            mockRepo.Verify(r => r.DeleteSession(sessionId, default), Times.Once);
        }

        [Test]
        public void Delete_ShouldDeleteTheUser() 
        {
            Guid userId = Guid.NewGuid();

            var mockContext = new Mock<IRequestContext>(MockBehavior.Strict);
            mockContext
                .Setup(ctx => ctx.Cancellation)
                .Returns(() => default);

            var mockRepo = new Mock<IUserRepository>(MockBehavior.Strict);
            mockRepo
                .Setup(r => r.Delete(userId, default))
                .Returns(Task.CompletedTask);

            var userManager = new UserManager(new Lazy<IUserRepository>(() => mockRepo.Object), mockContext.Object);

            Assert.DoesNotThrowAsync(() => userManager.Delete(userId));
            mockRepo.Verify(r => r.Delete(userId, default), Times.Once);
        }

        [Test]
        public void DeleteCurrent_ShouldDeleteTheCurrentUser() 
        {
            Guid 
                userId = Guid.NewGuid(),
                sessionId = Guid.NewGuid();

            var mockContext = new Mock<IRequestContext>(MockBehavior.Strict);
            mockContext
                .Setup(ctx => ctx.SessionId)
                .Returns(sessionId.ToString());
            mockContext
                .Setup(ctx => ctx.Cancellation)
                .Returns(() => default);

            var mockRepo = new Mock<IUserRepository>(MockBehavior.Strict);
            mockRepo
                .Setup(r => r.QueryBySession(sessionId, default))
                .Returns(Task.FromResult(new DAL.API.User { Id = userId }));
            mockRepo
                .Setup(r => r.Delete(userId, default))
                .Returns(Task.CompletedTask);

            var userManager = new UserManager(new Lazy<IUserRepository>(() => mockRepo.Object), mockContext.Object);

            Assert.DoesNotThrowAsync(() => userManager.DeleteCurrent());
            mockRepo.Verify(r => r.QueryBySession(sessionId, default), Times.Once);
            mockRepo.Verify(r => r.Delete(userId, default), Times.Once);
        }
    }
}
