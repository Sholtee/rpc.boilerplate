using System;
using System.Threading;
using System.Threading.Tasks;

using Solti.Utils.Rpc.Interfaces;

namespace DAL.API
{
    [ParameterValidatorAspect]
    public interface IUserRepository
    {
        Task<long> Create([NotNull, ValidateProperties] User user, [NotNull, LengthBetween(min: 5)] string password, CancellationToken cancellation = default);

        Task<Guid> CreateSession(long userId, CancellationToken cancellation = default);

        Task<User> QueryByCredentials([NotNull, LengthBetween(min: 5)] string emailOrUserName, [NotNull, LengthBetween(min: 5)] string password, CancellationToken cancellation = default);

        Task<User> QueryBySession(Guid sessionId, CancellationToken cancellation = default);

        Task<User> QueryById(long userId, CancellationToken cancellation = default);

        Task<PartialUserList> List(int skip, int count, CancellationToken cancellation = default);

        Task Delete(long userId, CancellationToken cancellation = default);

        Task DeleteSession(Guid sessionId, CancellationToken cancellation = default);
    }
}
