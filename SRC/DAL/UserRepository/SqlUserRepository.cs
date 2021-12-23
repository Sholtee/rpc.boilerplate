using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
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

#pragma warning disable CA1062 // Validate arguments of public methods

namespace DAL
{
    using API;
    using Properties;
    using Services.API;

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

        public async Task<Guid> Create(API.User user, string password, string[] groups, CancellationToken cancellation)
        {
            if (await Connection.ExistsAsync<DAL.User>(l => l.EmailOrUserName == user.EmailOrUserName && l.DeletedUtc == null, cancellation))
                throw new InvalidOperationException(Resources.USER_ALREADY_EXISTS);

            Guid[] groupIds = (await Connection.SelectAsync<DAL.Group>(grp => Sql.In(grp.Name, groups.Distinct()), cancellation))
                .Select(grp => grp.Id)
                .ToArray();
            if (groupIds.Length != groups.Length)
                throw new InvalidOperationException(Resources.INVALID_GROUP);

            using (IBulkedDbConnection bulk = Connection.CreateBulkedDbConnection())
            {
                DAL.User userEntry = new()
                {
                    EmailOrUserName = user.EmailOrUserName,
                    PasswordHash = HashPassword(password, GenerateSalt()),
                    FullName = user.FullName
                };
                bulk.Insert(userEntry);
                Debug.Assert(userEntry.Id != Guid.Empty);

                //
                // Don't use InsertAll() since it gives the same Id for each entry
                //

                foreach (Guid groupId in groupIds)
                {
                    DAL.UserGroup ug = new()
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
                    .Select<DAL.User>(u => new
                    {
                        u.EmailOrUserName,
                        u.PasswordHash,
                        u.Id,
                        u.FullName,
                        Roles = Sql.Custom<Roles>($"({roleQuery})")
                    })
                    .Where(u => u.DeletedUtc == null);

                static string GetFieldName<TTable>(Expression<Func<TTable, object>> selector) => typeof(TTable)
                    .GetModelMetadata()
                    .GetFieldDefinition(selector)
                    .FieldName;
            }
        }

        public async Task<API.UserEx> GetByCredentials(string emailOrUserName, string password, CancellationToken cancellation)
        {
            string sql = UserQueryBase
                .And<DAL.User>(u => u.EmailOrUserName == emailOrUserName)
                .ToMergedParamsSelectStatement();

            UserView? user = await Connection.QuerySingleOrDefaultAsync<UserView?>(sql, cancellation);
            if (user is null || !Verify(password, user.PasswordHash))
                throw new InvalidCredentialException(Resources.INVALID_CREDENTIALS);

            return Mapper.Map<API.UserEx>(user);
        }

        public async Task<API.UserEx> GetById(Guid userId, CancellationToken cancellation = default)
        {
            string sql = UserQueryBase
                .And<DAL.User>(u => u.Id == userId)
                .ToMergedParamsSelectStatement();

            UserView? user = await Connection.QuerySingleOrDefaultAsync<UserView?>(sql, cancellation);
            if (user is null)
                throw new InvalidOperationException(Resources.ENTRY_NOT_FOUND);

            return Mapper.Map<API.UserEx>(user);
        }

        public async Task DeleteById(Guid userId, CancellationToken cancellation)
        {
            if (await Connection.UpdateOnlyAsync<DAL.User>(new Dictionary<string, object> { [nameof(DAL.User.DeletedUtc)] = DateTime.UtcNow }, where: u => u.Id == userId && u.DeletedUtc == null, token: cancellation) is 0)
                throw new InvalidOperationException(Resources.ENTRY_NOT_FOUND);
        }

        public async Task<PartialUserList> List(int skip, int count, CancellationToken cancellation) => new PartialUserList
        {
            Entries = Mapper.Map<IList<UserEx>>(await Connection.SelectAsync<UserView>(UserQueryBase
                .OrderBy<DAL.User>(l => l.EmailOrUserName)
                .Limit(skip, count)
                .ToMergedParamsSelectStatement(), cancellation)),
            AllEntries = await Connection.CountAsync<DAL.User>(l => l.DeletedUtc == null, cancellation)
        };
    }
}
