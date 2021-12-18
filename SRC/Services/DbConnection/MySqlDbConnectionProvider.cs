using System;
using System.Data;

using MySql.Data.MySqlClient;
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

            OrmLiteConfig.DialectProvider = MySqlDialect.Provider; // thread static

            IDbConnection conn = new MySqlConnection(Config.ConnectionString);
            conn.Open();

            return conn;
        }
    }
}
