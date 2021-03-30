using System;
using System.Collections.Generic;
using System.Data;
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

    public class SqlUserRepository : IUserRepository
    {
        public IDbConnection Connection { get; }

        public IMapper Mapper { get; }

        public SqlUserRepository(IDbConnection connection) 
        {
            Connection = connection ?? throw new ArgumentNullException(nameof(connection));
            Mapper = new MapperConfiguration(cfg => cfg.AddProfiles(new Profile[]
            {
                new UserProfile()
            })).CreateMapper();
        }

        [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "Parameters are validated by aspects")]
        public async Task<long> Create(API.User user, string password, CancellationToken cancellation)
        {
            if (await Connection.ExistsAsync<DAL.Login>(l => l.EmailOrUserName == user.EmailOrUserName && l.Deleted == null, cancellation))
                throw new InvalidOperationException(Resources.USER_ALREADY_EXISTS);

            long loginId = await Connection.InsertAsync(new DAL.Login{ EmailOrUserName = user.EmailOrUserName, PasswordHash = HashPassword(password, GenerateSalt()) }, selectIdentity: true, cancellation);

            return await Connection.InsertAsync(new DAL.User { LoginId = loginId, FullName = user.FullName }, selectIdentity: true, cancellation);
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

        public async Task<API.User> QueryById(long userId, CancellationToken cancellation = default)
        {
            string sql = UserQueryBase
                .And<DAL.User>(u => u.Id == userId)
                .ToMergedParamsSelectStatement();

            UserView? user = await Connection.QuerySingleOrDefaultAsync<UserView?>(sql, cancellation);
            if (user is null)
                throw new InvalidOperationException(Resources.ENTRY_NOT_FOUND);

            return Mapper.Map<API.User>(user);
        }

        public async Task Delete(long userId, CancellationToken cancellation)
        {
            SqlExpression<DAL.User> userSelector = Connection
                .From<DAL.User>()
                .Select(u => u.LoginId)
                .Where(u => u.Id == userId);

            if (await Connection.UpdateOnlyAsync<DAL.Login>(new Dictionary<string, object> { [nameof(DAL.Login.Deleted)] = DateTime.UtcNow}, where: l => Sql.In(l.Id, userSelector), token: cancellation) == 0)
                throw new InvalidOperationException(Resources.ENTRY_NOT_FOUND);
        }
    }
}
