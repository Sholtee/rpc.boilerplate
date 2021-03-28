using System;

using ServiceStack.Data;
using ServiceStack.OrmLite;

namespace Services
{
    using API;

    public class MySqlDbConnectionFactoryProvider : IServiceProvider
    {
        public IConfig Config { get; }

        public MySqlDbConnectionFactoryProvider(IConfig config) => Config = config ?? throw new ArgumentNullException(nameof(config));

        public object GetService(Type serviceType)
        {
            if (serviceType != typeof(IDbConnectionFactory))
                throw new NotSupportedException();

            return new OrmLiteConnectionFactory(Config.ConnectionString, MySqlDialect.Provider)
            {
                AutoDisposeConnection = true
            };
        }
    }
}
