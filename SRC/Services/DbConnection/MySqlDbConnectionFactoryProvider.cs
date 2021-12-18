﻿using System;

using ServiceStack.Data;
using ServiceStack.OrmLite;

namespace Services
{
    using API;

    public class MySqlDbConnectionFactoryProvider : IServiceProvider
    {
        public IDbConnectionFactory ConnectionFactory { get; }

        public MySqlDbConnectionFactoryProvider(IConfig config)
        {
            if (config is null)
                throw new ArgumentNullException(nameof(config));

            ConnectionFactory = new OrmLiteConnectionFactory(config.ConnectionString, MySqlDialect.Provider)
            {
                AutoDisposeConnection = true
            };
        }

        public object GetService(Type serviceType)
        {
            if (serviceType != typeof(IDbConnectionFactory))
                throw new NotSupportedException();

            return ConnectionFactory;
        }
    }
}
