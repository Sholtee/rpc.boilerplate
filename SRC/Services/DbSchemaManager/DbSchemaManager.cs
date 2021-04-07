using System.Data;
using System.Reflection;

using Solti.Utils.OrmLite.Extensions;

namespace Services
{
    using API;

    public class SqlDbSchemaManager : IDbSchemaManager
    {
        public IDbConnection Connection { get; }

        public SqlDbSchemaManager(IDbConnection connection) => Connection = connection;

        public void CreateTables(params Assembly[] asmsToSearch)
        {
            var schema = new Schema(Connection, asmsToSearch);
            schema.CreateTablesCascaded();
        }

        public void DropTables(params Assembly[] asmsToSearch)
        {
            var schema = new Schema(Connection, asmsToSearch);
            schema.DropTablesCascaded();
        }
    }
}
