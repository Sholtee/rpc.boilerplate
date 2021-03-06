using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;

using AutoMapper;

using ServiceStack.OrmLite;
using ServiceStack.OrmLite.Dapper;

using Solti.Utils.OrmLite.Extensions;

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
        public async Task<Guid> Create(API.User user, string password, string[] groups, CancellationToken cancellation)
        {
            if (await Connection.ExistsAsync<DAL.Login>(l => l.EmailOrUserName == user.EmailOrUserName && l.DeletedUtc == null, cancellation))
                throw new InvalidOperationException(Resources.USER_ALREADY_EXISTS);

            Guid[] groupIds = (await Connection.SelectAsync<DAL.Group>(grp => Sql.In(grp.Name, groups.Distinct()), cancellation))
                .Select(grp => grp.Id)
                .ToArray();
            if (groupIds.Length != groups.Length)
                throw new InvalidOperationException(Resources.INVALID_GROUP);

            using (IBulkedDbConnection bulk = Connection.CreateBulkedDbConnection())
            {
                var loginEntry = new DAL.Login
                {
                    EmailOrUserName = user.EmailOrUserName,
                    PasswordHash = HashPassword(password, GenerateSalt())
                };
                bulk.Insert(loginEntry);
                Debug.Assert(loginEntry.Id != Guid.Empty);

                var userEntry = new DAL.User
                {
                    LoginId = loginEntry.Id,
                    FullName = user.FullName
                };
                bulk.Insert(userEntry);
                Debug.Assert(userEntry.Id != Guid.Empty);

                //
                // Don't use InsertAll() since it gives the same Id for each entry
                //

                foreach (Guid groupId in groupIds)
                {
                    var ug = new DAL.UserGroup
                    {
                        GroupId = groupId,
                        UserId = userEntry.Id
                    };
                    bulk.Insert(ug);
                    Debug.Assert(ug.Id != Guid.Empty);
                }

                await bulk.FlushAsync(cancellation);

                return userEntry.Id;
            }
        }

        private SqlExpression<DAL.User> UserQueryBase
        {
            get
            {
                string roleQuery = Connection
                    .From<DAL.Group>()
                    .Select($"BIT_OR({GetFieldName<DAL.Group>(grp => grp.Roles)})")
                    .Join<DAL.Group, DAL.UserGroup>((g, ug) => g.Id == ug.GroupId)
                    .Where<DAL.UserGroup, DAL.User>((ug, u) => ug.UserId == u.Id)
                    .ToMergedParamsSelectStatement();

                return Connection
                    .From<DAL.User>()
                    .Join<DAL.User, DAL.Login>((u, l) => u.LoginId == l.Id && l.DeletedUtc == null)
                    .Select<DAL.Login, DAL.User>((l, u) => new
                    {
                        l.EmailOrUserName,
                        l.PasswordHash,
                        u.Id,
                        u.FullName,
                        Roles = Sql.Custom<Roles>($"({roleQuery})")
                    });

                static string GetFieldName<TTable>(Expression<Func<TTable, object>> selector) => typeof(TTable)
                    .GetModelMetadata()
                    .GetFieldDefinition(selector)
                    .FieldName;
            }
        }

        public async Task<API.UserEx> QueryByCredentials(string emailOrUserName, string password, CancellationToken cancellation)
        {
            string sql = UserQueryBase
                .And<DAL.Login>(l => l.EmailOrUserName == emailOrUserName)
                .ToMergedParamsSelectStatement();

            UserView? user = await Connection.QuerySingleOrDefaultAsync<UserView?>(sql, cancellation);
            if (user is null || !Verify(password, user.PasswordHash))
                throw new InvalidCredentialException(Resources.INVALID_CREDENTIALS);

            return Mapper.Map<API.UserEx>(user);
        }

        public async Task<API.UserEx> QueryById(Guid userId, CancellationToken cancellation = default)
        {
            string sql = UserQueryBase
                .And<DAL.User>(u => u.Id == userId)
                .ToMergedParamsSelectStatement();

            UserView? user = await Connection.QuerySingleOrDefaultAsync<UserView?>(sql, cancellation);
            if (user is null)
                throw new InvalidOperationException(Resources.ENTRY_NOT_FOUND);

            return Mapper.Map<API.UserEx>(user);
        }

        public async Task<API.UserEx> QueryBySession(Guid sessionId, CancellationToken cancellation)
        {
            string sql = UserQueryBase
                .Join<DAL.User, DAL.Session>((u, s) => u.Id == s.UserId && s.Id == sessionId && s.ExpiredUtc > DateTime.UtcNow)
                .ToMergedParamsSelectStatement();

            UserView? user = await Connection.QuerySingleOrDefaultAsync<UserView?>(sql, cancellation);
            if (user is null)
                throw new InvalidCredentialException(Resources.INVALID_CREDENTIALS);

            await Connection.UpdateOnlyAsync<DAL.Session>(new Dictionary<string, object> { [nameof(DAL.Session.ExpiredUtc)] = DateTime.UtcNow.AddMinutes(Config.Server.SessionTimeoutInMinutes) }, where: s => s.Id == sessionId, token: cancellation);

            return Mapper.Map<API.UserEx>(user);
        }

        public async Task Delete(Guid userId, CancellationToken cancellation)
        {
            SqlExpression<DAL.User> userSelector = Connection
                .From<DAL.User>()
                .Select(u => u.LoginId)
                .Where(u => u.Id == userId);

            if (await Connection.UpdateOnlyAsync<DAL.Login>(new Dictionary<string, object> { [nameof(DAL.Login.DeletedUtc)] = DateTime.UtcNow }, where: l => Sql.In(l.Id, userSelector), token: cancellation) == 0)
                throw new InvalidOperationException(Resources.ENTRY_NOT_FOUND);

            await Connection.UpdateOnlyAsync<DAL.Session>(new Dictionary<string, object> { [nameof(DAL.Session.ExpiredUtc)] = DateTime.UtcNow }, where: s => s.UserId == userId && s.ExpiredUtc > DateTime.UtcNow, token: cancellation);
        }

        public async Task<PartialUserList> List(int skip, int count, CancellationToken cancellation) => new PartialUserList
        {
            Entries = Mapper.Map<IList<UserEx>>(await Connection.SelectAsync<UserView>(UserQueryBase
                .OrderBy<DAL.Login>(l => l.EmailOrUserName)
                .Limit(skip, count)
                .ToMergedParamsSelectStatement(), cancellation)),
            AllEntries = await Connection.CountAsync<DAL.Login>(l => l.DeletedUtc == null, cancellation)
        };

        public async Task<Guid> CreateSession(Guid userId, CancellationToken cancellation = default)
        {
            string sql = Connection
                .From<DAL.Session>()
                .Select()
                .Where(s => s.UserId == userId && s.ExpiredUtc > DateTime.UtcNow)
                .ToMergedParamsSelectStatement();

            DAL.Session? sessionEntry = await Connection.QuerySingleOrDefaultAsync<DAL.Session?>(sql, cancellation);
            if (sessionEntry is not null)
                await Connection.UpdateOnlyAsync<DAL.Session>(new Dictionary<string, object> { [nameof(DAL.Session.ExpiredUtc)] = DateTime.UtcNow.AddMinutes(Config.Server.SessionTimeoutInMinutes) }, where: s => s.Id == sessionEntry.Id, token: cancellation);
            else
            {
                sessionEntry = new DAL.Session
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
            await Connection.UpdateOnlyAsync<DAL.Session>(new Dictionary<string, object> { [nameof(DAL.Session.ExpiredUtc)] = DateTime.UtcNow }, where: s => s.Id == sessionId && s.ExpiredUtc > DateTime.UtcNow, token: cancellation);
    }
}
