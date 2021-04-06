using System;
using System.Threading;
using System.Threading.Tasks;

using Solti.Utils.Rpc.Interfaces;

namespace Services
{
    using API;
    using DAL.API;

    public class RoleManager : IRoleManager, IAsyncRoleManager
    {
        private readonly Lazy<IUserRepository> FUserRepository;
        public IUserRepository UserRepository => FUserRepository.Value;

        public RoleManager(Lazy<IUserRepository> userRepository) => FUserRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));

        public Enum GetAssignedRoles(string? sessionId) => GetAssignedRolesAsync(sessionId, default).GetAwaiter().GetResult();

        public async Task<Enum> GetAssignedRolesAsync(string? sessionId, CancellationToken cancellation)
        {
            if (sessionId is null)
                return Roles.AnonymousUser;

            UserEx user = await UserRepository.QueryBySession(Guid.Parse(sessionId), cancellation);

            return user.Roles | Roles.AuthenticatedUser;
        }
    }
}
