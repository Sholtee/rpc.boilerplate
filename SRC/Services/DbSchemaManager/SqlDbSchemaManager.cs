using System;
using System.Data;
using System.Linq;
using System.Reflection;

using Solti.Utils.DI.Interfaces;
using Solti.Utils.OrmLite.Extensions;

namespace Services
{
    using API;

    public sealed class SqlDbSchemaManager : Schema, IDbSchemaManager
    {
        public SqlDbSchemaManager(IInjector injector, string? dbTag) : base(injector.Get<IDbConnection>(dbTag), AppDomain
            .CurrentDomain
            .GetAssemblies()
            .Where(asm =>
            {
                DataAccessAssemblyAttribute? daaa = asm.GetCustomAttribute<DataAccessAssemblyAttribute>();
                return daaa is not null && daaa.DbTag == dbTag;
            })
            .ToArray())
        {
        }
    }
}
