using System;

using ServiceStack.Data;
using ServiceStack.OrmLite;

namespace Services
{
    public class SQLiteDbConnectionFactoryProvider : IServiceProvider
    {
        public IDbConnectionFactory ConnectionFactory { get; }

        public SQLiteDbConnectionFactoryProvider()
        {
            ConnectionFactory = new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider)
            {
                AutoDisposeConnection = true
            };
        }

        public const string ServiceName = "memory";

        public object GetService(Type serviceType)
        {
            if (serviceType != typeof(IDbConnectionFactory))
                throw new NotSupportedException();

            return ConnectionFactory;
        }
    }
}
