using System;
using System.IO;

using ServiceStack.Redis;

using Solti.Utils.DI.Interfaces;
using Solti.Utils.Primitives.Patterns;

namespace Services
{
    using API;
    
    public class RedisCache : Disposable, ICache
    {
        private static readonly string KEY_PREFIX = $"{Path.GetFileNameWithoutExtension(typeof(RedisCache).Assembly.Location)}_";

        private readonly IRedisClient FClient;

        private class Entry<T>
        {
            #pragma warning disable CS8618
            public T Data { get; set; }
            #pragma warning restore CS8618
            public TimeSpan ExpiresIn { get; set; }
        }

        private static string GetKey(string key) => KEY_PREFIX + key;

        [ServiceActivator]
        public RedisCache(IConfig<RedisConfig> config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            FClient = new RedisClient(config.Value.Host, config.Value.Port);
        }

        public RedisCache(string host, int port) => FClient = new RedisClient(host, port);

        protected override void Dispose(bool disposeManaged)
        {
            if (disposeManaged) FClient?.Dispose();

            base.Dispose(disposeManaged);
        }

        public bool Add<T>(string key, T val, TimeSpan expiresIn)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            return FClient.Add(GetKey(key), new Entry<T> { Data = val, ExpiresIn = expiresIn }, expiresIn);
        }

        public void Set<T>(string key, T val, TimeSpan expiresIn)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            FClient.Set(GetKey(key), new Entry<T> { Data = val, ExpiresIn = expiresIn }, expiresIn);
        }

        public bool Remove(string key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            return FClient.Remove(GetKey(key));
        }

        public bool TryGetValue<T>(string key, out T val)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            key = GetKey(key);

            using (FClient.AcquireLock($"{key}_lock", TimeSpan.FromSeconds(10)))
            {
                Entry<T>? entry = FClient.Get<Entry<T>>(key);
                if (entry != null)
                {
                    FClient.ExpireEntryIn(key, entry.ExpiresIn);
                    val = entry.Data;
                    return true;
                }
            }

            val = default!;
            return false;
        }

        public void Clear() => FClient.RemoveByPattern($"{KEY_PREFIX}*");
    }
}
