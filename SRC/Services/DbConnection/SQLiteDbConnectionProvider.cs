using System;
using System.Data;

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

            OrmLiteConnectionFactory connectionFactory = new("Data Source=MyApp_InMemoryDb;Mode=Memory;Cache=Shared", SqliteDialect.Provider);
            return connectionFactory.OpenDbConnection();
        }
    }
}
