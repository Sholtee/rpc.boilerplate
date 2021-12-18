using System;
using System.Data;
using System.Data.SQLite;

using ServiceStack.OrmLite;

namespace Services
{
    public class SQLiteDbConnectionProvider : IServiceProvider
    {
        public const string ServiceName = "memory";

        public object GetService(Type serviceType)
        {
            if (serviceType != typeof(IDbConnection))
                throw new NotSupportedException();

            OrmLiteConfig.DialectProvider = SqliteDialect.Provider; // thread static

            SQLiteConnectionStringBuilder connectionString = new();
            connectionString.DataSource = ":memory:";
            connectionString.Add("cache", "shared");

            IDbConnection conn = new SQLiteConnection(connectionString.ToString());
            conn.Open();

            return conn;
        }
    }
}
