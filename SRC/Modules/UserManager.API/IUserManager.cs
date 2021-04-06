﻿using System;
using System.Data;
using System.Threading.Tasks;

using Solti.Utils.Rpc.Interfaces;

namespace Modules.API
{
    using Services.API;

    [ParameterValidatorAspect, RoleValidatorAspect, TransactionAspect]
    public interface IUserManager
    {
        [RequiredRoles(Roles.Admin), Transactional]
        Task<Guid> Create([NotNull, ValidateProperties] User user, [NotNull, LengthBetween(min: 5)] string pw);

        [RequiredRoles(Roles.Admin), Transactional]
        Task Delete(Guid userId);

        [RequiredRoles(Roles.AuthenticatedUser), Transactional]
        Task DeleteCurrent();

        [RequiredRoles(Roles.Admin), Transactional(IsolationLevel = IsolationLevel.Serializable)]
        Task<PartialUserList> List(int skip, int count);

        [RequiredRoles(Roles.AnonymousUser)]
        Task<Guid> Login([NotNull, LengthBetween(min: 5)] string emailOrUserName, [NotNull, LengthBetween(min: 5)] string pw);

        [RequiredRoles(Roles.AuthenticatedUser)]
        Task Logout();
    }
}
