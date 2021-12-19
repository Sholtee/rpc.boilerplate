using System;
using System.Threading;
using System.Threading.Tasks;

using Solti.Utils.Rpc.Interfaces;

namespace DAL.API
{
    [ParameterValidatorAspect, LoggerAspect(typeof(ServiceMethodScopeLogger), typeof(StopWatchLogger), typeof(ExceptionLogger))]
    public interface ISessionRepository
    {
        Task<Guid> GetOrCreate(Guid userId, CancellationToken cancellation = default);

        Task ExpireById(Guid sessionId, CancellationToken cancellation = default);

        Task ExpireByUserId(Guid userId, CancellationToken cancellation = default);

        Task<Guid> GetUserId(Guid sessionId, CancellationToken cancellation = default);

        Task Clean(CancellationToken cancellation = default);
    }
}
