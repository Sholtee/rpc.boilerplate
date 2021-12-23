using System;
using System.Data;

using ServiceStack.OrmLite;

namespace Services
{
    using API;

    public class MySqlDbConnectionProvider : IServiceProvider
    {
        public DatabaseConfig Config { get; }

        public MySqlDbConnectionProvider(IConfig<DatabaseConfig> config) => Config = (config ?? throw new ArgumentNullException(nameof(config))).Value;

        public object GetService(Type serviceType)
        {
            if (serviceType != typeof(IDbConnection))
                throw new NotSupportedException();

            OrmLiteConnectionFactory connectionFactory = new(Config.ConnectionString, MySqlDialect.Provider);
            return connectionFactory.OpenDbConnection();
        }
    }
}
