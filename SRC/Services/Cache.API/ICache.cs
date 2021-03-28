using System;
using System.Diagnostics.CodeAnalysis;

namespace Services.API
{
    public interface ICache
    {
        bool TryGetValue<T>(string key, out T val);

        bool Add<T>(string key, T val, TimeSpan expiresIn);

        [SuppressMessage("Naming", "CA1716:Identifiers should not match keywords")]
        void Set<T>(string key, T val, TimeSpan expiresIn);

        bool Remove(string key);

        void Clear();
    }
}