using System;
using System.Data;

using ServiceStack.Data;

namespace Services
{
    public class SqlConnectionProvider : IServiceProvider
    {
        public IDbConnectionFactory DbConnectionFactory { get; }

        public SqlConnectionProvider(IDbConnectionFactory dbConnectionFactory) => DbConnectionFactory = dbConnectionFactory ?? throw new ArgumentNullException(nameof(dbConnectionFactory));

        public object GetService(Type serviceType)
        {
            if (serviceType != typeof(IDbConnection)) 
                throw new NotSupportedException();

            return DbConnectionFactory.OpenDbConnection();
        }
    }
}
