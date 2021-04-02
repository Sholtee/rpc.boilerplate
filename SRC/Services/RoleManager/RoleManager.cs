using System;
using System.Security.Authentication;

using Solti.Utils.Rpc.Interfaces;

namespace Services
{
    using API;
    using DAL.API;
    using Properties;

    public class RoleManager : IRoleManager
    {
        public ICache Cache { get; }

        public RoleManager(ICache cache) => Cache = cache ?? throw new ArgumentNullException(nameof(cache));

        public Enum GetAssignedRoles(string? sessionId)
        {
            if (sessionId is null)
                return Roles.AnonymousUser;

            if (!Cache.TryGetValue(sessionId, out User _))
                throw new InvalidCredentialException(Resources.INVALID_SESSION);

            return Roles.AuthenticatedUser;
        }
    }
}
