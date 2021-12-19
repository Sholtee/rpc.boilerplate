using System;
using System.Threading;
using System.Threading.Tasks;

using Solti.Utils.Rpc.Interfaces;

namespace Services
{
    using API;
    using DAL.API;

    public class RoleManager : IRoleManager
    {
        private readonly Lazy<IUserRepository> FUserRepository;
        public IUserRepository UserRepository => FUserRepository.Value;

        private readonly Lazy<ISessionRepository> FSessionRepository;
        public ISessionRepository SessionRepository => FSessionRepository.Value;

        public RoleManager(Lazy<IUserRepository> userRepository, Lazy<ISessionRepository> sessionRepository)
        {
            FUserRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            FSessionRepository = sessionRepository ?? throw new ArgumentNullException(nameof(sessionRepository));
        }

        public Enum GetAssignedRoles(string? sessionId) => GetAssignedRolesAsync(sessionId, default).GetAwaiter().GetResult();

        public async Task<Enum> GetAssignedRolesAsync(string? sessionId, CancellationToken cancellation)
        {
            if (sessionId is null)
                return Roles.AnonymousUser;

            UserEx user = await UserRepository.GetById
            (
                await SessionRepository.GetUserId(Guid.Parse(sessionId), cancellation),
                cancellation
            );

            return user.Roles | Roles.AuthenticatedUser;
        }
    }
}
