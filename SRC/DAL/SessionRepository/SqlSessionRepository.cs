using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using ServiceStack.OrmLite;
// using ServiceStack.OrmLite.Dapper;  // SQLite + Dapper == InvalidCastException (in case of Guid)

using Solti.Utils.DI.Interfaces;

namespace DAL
{
    using API;
    using Properties;
    using Services.API;

    public class SqlSessionRepository: ISessionRepository
    {
        public IDbConnection Connection { get; }

        public SessionConfig Config { get; }

        public SqlSessionRepository([Options(Name = "memory")] IDbConnection connection, IConfig<SessionConfig> config)
        {
            Connection = connection ?? throw new ArgumentNullException(nameof(connection));
            Config = (config ?? throw new ArgumentNullException(nameof(config))).Value;
        }

        public Task Clean(CancellationToken cancellation) => Connection.DeleteAsync<DAL.Session>(sess => sess.ExpiresUtc <= DateTime.UtcNow, token: cancellation);

        public async Task<Guid> GetOrCreate(Guid userId, CancellationToken cancellation)
        {
            DAL.Session? session = 
            (
                await Connection.SelectAsync<DAL.Session>
                (
                    Connection
                        .From<DAL.Session>()
                        .Select()
                        .Where(s => s.UserId == userId && s.ExpiresUtc > DateTime.UtcNow)
                        .ToMergedParamsSelectStatement(),
                    cancellation
                )
            ).SingleOrDefault();

            DateTime expiry = DateTime.UtcNow.AddMinutes(Config.TimeoutInMinutes);

            if (session is not null)
                await Connection.UpdateOnlyAsync<DAL.Session>(new Dictionary<string, object> { [nameof(DAL.Session.ExpiresUtc)] = expiry }, where: s => s.Id == session.Id, token: cancellation);
            else
            {
                session = new()
                {
                    UserId = userId,
                    CreatedUtc = DateTime.UtcNow,
                    ExpiresUtc = expiry
                };

                await Connection.InsertAsync(session, token: cancellation);
                Debug.Assert(session.Id != Guid.Empty);
            }

            return session.Id;
        }

        public async Task ExpireById(Guid sessionId, CancellationToken cancellation)
        {
            if (await Connection.UpdateOnlyAsync<DAL.Session>(new Dictionary<string, object> { [nameof(DAL.Session.ExpiresUtc)] = DateTime.UtcNow }, where: s => s.Id == sessionId && s.ExpiresUtc > DateTime.UtcNow, token: cancellation) is 0)
                throw new InvalidOperationException(Resources.INVALID_SESSION_ID);
        }

        public async Task ExpireByUserId(Guid userId, CancellationToken cancellation)
        {
            if (await Connection.UpdateOnlyAsync<DAL.Session>(new Dictionary<string, object> { [nameof(DAL.Session.ExpiresUtc)] = DateTime.UtcNow }, where: s => s.UserId == userId && s.ExpiresUtc > DateTime.UtcNow, token: cancellation) is 0)
                throw new InvalidOperationException(Resources.INVALID_USER_ID);
        }

        public async Task<Guid> GetUserId(Guid sessionId, CancellationToken cancellation) => await Connection.ScalarAsync<Guid?>
        (
            Connection
                .From<DAL.Session>()
                .Select(session => session.UserId)
                .Where(session => session.Id == sessionId),
            token: cancellation
        ) ?? throw new InvalidOperationException(Resources.INVALID_SESSION_ID);
    }
}
