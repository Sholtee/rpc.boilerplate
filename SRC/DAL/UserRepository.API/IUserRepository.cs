using System;
using System.Threading;
using System.Threading.Tasks;

using Solti.Utils.Rpc.Interfaces;

namespace DAL.API
{
    [ParameterValidatorAspect, DataServiceLoggerAspect]
    public interface IUserRepository
    {
        [Loggers(typeof(ServiceMethodScopeLogger), typeof(StopWatchLogger), typeof(ExceptionLogger))] // don't log parameteres (would contain sensitive data)
        Task<Guid> Create([NotNull, ValidateProperties] User user, [NotNull, LengthBetween(min: 5)] string password, string[] groups, CancellationToken cancellation = default);

        [Loggers(typeof(ServiceMethodScopeLogger), typeof(StopWatchLogger), typeof(ExceptionLogger))] // don't log parameteres (would contain sensitive data)
        Task<UserEx> GetByCredentials([NotNull, LengthBetween(min: 5)] string emailOrUserName, [NotNull, LengthBetween(min: 5)] string password, CancellationToken cancellation = default);

        Task<UserEx> GetById(Guid userId, CancellationToken cancellation = default);

        Task<PartialUserList> List(int skip, int count, CancellationToken cancellation = default);

        Task DeleteById(Guid userId, CancellationToken cancellation = default);
    }
}
