using System;
using System.Data;

using ServiceStack.OrmLite;

namespace Services
{
    using API;

    public class MySqlDbConnectionProvider : IServiceProvider
    {
        public IConfig Config { get; }

        public MySqlDbConnectionProvider(IConfig config) => Config = config ?? throw new ArgumentNullException(nameof(config));

        public object GetService(Type serviceType)
        {
            if (serviceType != typeof(IDbConnection))
                throw new NotSupportedException();

            OrmLiteConnectionFactory connectionFactory = new(Config.Database.ConnectionString, MySqlDialect.Provider);
            return connectionFactory.OpenDbConnection();
        }
    }
}
