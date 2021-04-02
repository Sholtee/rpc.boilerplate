using System;
using System.Threading.Tasks;

using Solti.Utils.Rpc.Interfaces;

namespace Modules.API
{
    using Services.API;

    [ParameterValidatorAspect, TransactionAspect, RoleValidatorAspect]
    public interface IFelhasznaloKezelo
    {
        [RequiredRoles(Roles.AuthenticatedUser), Transactional]
        Task<long> Letrehoz([NotNull, LengthBetween(min: 5)] string user, [NotNull, LengthBetween(min: 5)] string pw);

        [RequiredRoles(Roles.AnonymousUser)]
        Task<Guid> Belep([NotNull, LengthBetween(min: 5)] string user, [NotNull, LengthBetween(min: 5)] string pw);

        [RequiredRoles(Roles.AuthenticatedUser)]
        void Kilep();
    }
}
