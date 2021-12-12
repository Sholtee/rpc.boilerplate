using System;
using System.Data;
using System.Linq;
using System.Reflection;

using Solti.Utils.OrmLite.Extensions;

namespace Services
{
    using API;

    public class SqlDbSchemaManager : Schema, IDbSchemaManager
    {
        public SqlDbSchemaManager(IDbConnection connection) : base(connection ?? throw new ArgumentNullException(nameof(connection)), AppDomain
            .CurrentDomain
            .GetAssemblies()
            .Where(asm => asm.GetCustomAttribute<DataAccessAssemblyAttribute>() is not null)
            .ToArray())
        {
        }
    }
}
