using System;
using System.Runtime.Caching;

namespace Services
{
    using API;

    public class MemoryCache : ICache
    {
        private readonly System.Runtime.Caching.MemoryCache FBackend = System.Runtime.Caching.MemoryCache.Default;

        public bool Add<T>(string key, T val, TimeSpan expiresIn) => FBackend.Add(key, val, new CacheItemPolicy 
        {
            AbsoluteExpiration = ObjectCache.InfiniteAbsoluteExpiration,
            SlidingExpiration = expiresIn
        });

        public void Set<T>(string key, T val, TimeSpan expiresIn) => FBackend.Set(key, val, new CacheItemPolicy
        {
            AbsoluteExpiration = ObjectCache.InfiniteAbsoluteExpiration,
            SlidingExpiration = expiresIn
        });

        public void Clear() => FBackend.Trim(100);

        public bool Remove(string key) => FBackend.Remove(key) != null;

        public bool TryGetValue<T>(string key, out T val)
        {
            object? result = FBackend.Get(key);
            
            if (result == null) 
            {
                val = default!;
                return false;
            }

            val = (T) result;
            return true;
        }
    }
}
