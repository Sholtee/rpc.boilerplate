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

            IDbConnection conn = new SQLiteConnection("Data Source=MyApp_InMemoryDb;Mode=Memory;Cache=Shared");
            conn.Open();

            return conn;
        }
    }
}
