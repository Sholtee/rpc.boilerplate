using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

using ServiceStack.OrmLite;
using Solti.Utils.OrmLite.Extensions;

namespace Services
{
    using API;

    public class SqlDbSchemaManager : IDbSchemaManager
    {
        public IDbConnection Connection { get; }

        public SqlDbSchemaManager(IDbConnection connection) => Connection = connection;

        private static Type[] GetTablesFrom(IEnumerable<Assembly> asmsToSearch) => asmsToSearch
            .SelectMany(asm => asm.GetTypes().Where(t => t.GetCustomAttribute<DataTableAttribute>(inherit: false) != null))
            .ToArray();

        public void CreateTables(params Assembly[] asmsToSearch)
        {
            var schema = new Schema(Connection, GetTablesFrom(asmsToSearch));
            schema.CreateTablesCascaded();
        }

        public void DropTables(params Assembly[] asmsToSearch)
        {
            var schema = new Schema(Connection, GetTablesFrom(asmsToSearch));
            schema.DropTablesCascaded();
        }
    }
}
