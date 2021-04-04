using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;

using AutoMapper;

using ServiceStack.OrmLite;
using ServiceStack.OrmLite.Dapper;

using static BCrypt.Net.BCrypt;

namespace DAL
{
    using API;
    using Properties;
    using Services.API;

    public class SqlUserRepository : IUserRepository
    {
        public IDbConnection Connection { get; }

        public IConfig Config { get; }

        public IMapper Mapper { get; }

        public SqlUserRepository(IDbConnection connection, IConfig config) 
        {
            Connection = connection ?? throw new ArgumentNullException(nameof(connection));
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Mapper = new MapperConfiguration(cfg => cfg.AddProfiles(new Profile[]
            {
                new UserProfile()
            })).CreateMapper();
        }

        [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "Parameters are validated by aspects")]
        public async Task<Guid> Create(API.User user, string password, CancellationToken cancellation)
        {
            if (await Connection.ExistsAsync<DAL.Login>(l => l.EmailOrUserName == user.EmailOrUserName && l.Deleted == null, cancellation))
                throw new InvalidOperationException(Resources.USER_ALREADY_EXISTS);

            var loginEntry = new DAL.Login
            {
                EmailOrUserName = user.EmailOrUserName,
                PasswordHash = HashPassword(password, GenerateSalt())
            };
            await Connection.InsertAsync(loginEntry, token: cancellation);
            Debug.Assert(loginEntry.Id != Guid.Empty);

            var userEntry = new DAL.User 
            {
                LoginId = loginEntry.Id, 
                FullName = user.FullName 
            };
            await Connection.InsertAsync(userEntry, selectIdentity: true, cancellation);
            Debug.Assert(userEntry.Id != Guid.Empty);

            return userEntry.Id;
        }

        private SqlExpression<DAL.User> UserQueryBase => Connection
            .From<DAL.User>()
            .Join<DAL.User, DAL.Login>((u, l) => u.LoginId == l.Id && l.Deleted == null)
            .Select<DAL.Login, DAL.User>((l, u) => new { l.EmailOrUserName, l.PasswordHash, u.Id, u.FullName });

        public async Task<API.User> QueryByCredentials(string emailOrUserName, string password, CancellationToken cancellation)
        {
            string sql = UserQueryBase
                .And<DAL.Login>(l => l.EmailOrUserName == emailOrUserName)
                .ToMergedParamsSelectStatement();

            UserView? user = await Connection.QuerySingleOrDefaultAsync<UserView?>(sql, cancellation);
            if (user is null || !Verify(password, user.PasswordHash))
                throw new InvalidCredentialException(Resources.INVALID_CREDENTIALS);

            return Mapper.Map<API.User>(user);
        }

        public async Task<API.User> QueryById(Guid userId, CancellationToken cancellation = default)
        {
            string sql = UserQueryBase
                .And<DAL.User>(u => u.Id == userId)
                .ToMergedParamsSelectStatement();

            UserView? user = await Connection.QuerySingleOrDefaultAsync<UserView?>(sql, cancellation);
            if (user is null)
                throw new InvalidOperationException(Resources.ENTRY_NOT_FOUND);

            return Mapper.Map<API.User>(user);
        }

        public async Task<API.User> QueryBySession(Guid sessionId, CancellationToken cancellation)
        {
            string sql = UserQueryBase
                .Join<DAL.User, DAL.UserSession>((u, s) => u.Id == s.UserId && s.Id == sessionId && s.ExpiredUtc > DateTime.UtcNow)
                .ToMergedParamsSelectStatement();

            UserView? user = await Connection.QuerySingleOrDefaultAsync<UserView?>(sql, cancellation);
            if (user is null)
                throw new InvalidCredentialException(Resources.INVALID_CREDENTIALS);

            await Connection.UpdateOnlyAsync<DAL.UserSession>(new Dictionary<string, object> { [nameof(DAL.UserSession.ExpiredUtc)] = DateTime.UtcNow.AddMinutes(Config.Server.SessionTimeoutInMinutes) }, where: s => s.Id == sessionId, token: cancellation);

            return Mapper.Map<API.User>(user);
        }

        public async Task Delete(Guid userId, CancellationToken cancellation)
        {
            SqlExpression<DAL.User> userSelector = Connection
                .From<DAL.User>()
                .Select(u => u.LoginId)
                .Where(u => u.Id == userId);

            if (await Connection.UpdateOnlyAsync<DAL.Login>(new Dictionary<string, object> { [nameof(DAL.Login.Deleted)] = DateTime.UtcNow }, where: l => Sql.In(l.Id, userSelector), token: cancellation) == 0)
                throw new InvalidOperationException(Resources.ENTRY_NOT_FOUND);

            await Connection.UpdateOnlyAsync<DAL.UserSession>(new Dictionary<string, object> { [nameof(DAL.UserSession.ExpiredUtc)] = DateTime.UtcNow }, where: s => s.UserId == userId && s.ExpiredUtc > DateTime.UtcNow, token: cancellation);
        }

        public async Task<PartialUserList> List(int skip, int count, CancellationToken cancellation) => new PartialUserList
        {
            Entries = await Connection.SelectAsync<API.User>(UserQueryBase
                .OrderBy<DAL.Login>(l => l.EmailOrUserName)
                .Limit(skip, count)
                .ToMergedParamsSelectStatement(), cancellation),
            AllEntries = await Connection.CountAsync<DAL.Login>(l => l.Deleted == null, cancellation)
        };

        public async Task<Guid> CreateSession(Guid userId, CancellationToken cancellation = default)
        {
            string sql = Connection
                .From<DAL.UserSession>()
                .Select()
                .Where(s => s.UserId == userId && s.ExpiredUtc > DateTime.UtcNow)
                .ToMergedParamsSelectStatement();

            DAL.UserSession? sessionEntry = await Connection.QuerySingleOrDefaultAsync<DAL.UserSession?>(sql, cancellation);
            if (sessionEntry is not null)
                await Connection.UpdateOnlyAsync<DAL.UserSession>(new Dictionary<string, object> { [nameof(DAL.UserSession.ExpiredUtc)] = DateTime.UtcNow.AddMinutes(Config.Server.SessionTimeoutInMinutes) }, where: s => s.Id == sessionEntry.Id, token: cancellation);
            else
            {
                sessionEntry = new UserSession
                {
                    CreatedUtc = DateTime.UtcNow,
                    UserId     = userId,
                    ExpiredUtc = DateTime.UtcNow.AddMinutes(Config.Server.SessionTimeoutInMinutes)
                };
                await Connection.InsertAsync(sessionEntry, token: cancellation);
                Debug.Assert(sessionEntry.Id != Guid.Empty);
            }

            return sessionEntry.Id;
        }

        public async Task DeleteSession(Guid sessionId, CancellationToken cancellation = default) =>
            await Connection.UpdateOnlyAsync<DAL.UserSession>(new Dictionary<string, object> { [nameof(DAL.UserSession.ExpiredUtc)] = DateTime.UtcNow }, where: s => s.Id == sessionId && s.ExpiredUtc > DateTime.UtcNow, token: cancellation);
    }
}
