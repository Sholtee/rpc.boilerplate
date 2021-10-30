using System;
using System.Threading;

using NUnit.Framework;

using Solti.Utils.DI;
using Solti.Utils.DI.Interfaces;

using Tests.Base;

namespace Services.Tests
{
    using API;
 
    public abstract class CacheTestsBase<TImpl>: TestsBase where TImpl: class, ICache
    {
        const string key = nameof(key);
        const int val = 1986;

        public ICache Cache { get; set; }

        public override void Setup()
        {
            base.Setup();
            Cache = Injector.Instantiate<TImpl>();
        }

        public override void TearDown() 
        {
            base.TearDown();
            Cache?.Clear();
        }

        [Test]
        public void Add_ShouldAddANewEntry()
        {
            Assert.That(Cache!.Add(key, val, TimeSpan.FromDays(1)));
            Assert.That(Cache.TryGetValue(key, out int ret));
            Assert.That(ret, Is.EqualTo(val));
        }

        [Test]
        public void Add_ShouldNotOverwriteExistingData() 
        {
            Assert.That(Cache!.Add(key, val, TimeSpan.FromDays(1)));

            Assert.False(Cache!.Add(key, val, TimeSpan.FromDays(1)));
            Assert.False(Cache!.Add(key, "cica", TimeSpan.FromDays(1)));
        }

        [Test]
        public void Set_ShouldAddANewEntry()
        {
            Assert.DoesNotThrow(() => Cache!.Set(key, val, TimeSpan.FromDays(1)));
            Assert.That(Cache.TryGetValue(key, out int ret));
            Assert.That(ret, Is.EqualTo(val));
        }

        [Test]
        public void Set_ShouldOverwriteExistingData()
        {
            Assert.DoesNotThrow(() => Cache!.Set(key, val, TimeSpan.FromDays(1)));
            Assert.DoesNotThrow(() => Cache!.Set(key, "cica", TimeSpan.FromDays(1)));
            Assert.That(Cache.TryGetValue(key, out string ret));
            Assert.That(ret, Is.EqualTo("cica"));
        }

        [Test]
        public void PersistedData_ShouldBeAccessibleAcrossInstances() 
        {
            Assert.That(Cache!.Add(key, val, TimeSpan.FromDays(1)));

            using IInjector injector = ScopeFactory!.CreateScope();
            ICache second = injector.Get<ICache>();

            Assert.That(second.TryGetValue(key, out int result));
            Assert.That(result, Is.EqualTo(val));
        }

        [Test]
        public void PersistedData_ShouldTimeout() 
        {
            Assert.That(Cache!.Add(key, val, TimeSpan.FromMilliseconds(10)));
            Thread.Sleep(20);
            Assert.False(Cache.TryGetValue(key, out int _));
        }

        [Test]
        public void Remove_ShouldRemoveTheDesiredEntry() 
        {
            Assert.That(Cache!.Add(key, val, TimeSpan.FromDays(1)));
            Assert.That(Cache.Remove(key));
            Assert.False(Cache.Remove(key));
            Assert.False(Cache.TryGetValue(key, out int _));
        }

        [Test]
        public void TryGetValue_ShouldNotRemoveTheEntry() 
        {
            Assert.That(Cache!.Add(key, val, TimeSpan.FromDays(1)));

            for (int i = 0; i < 5; i++)
                Assert.That(Cache.TryGetValue(key, out int _));

            using IInjector injector = ScopeFactory!.CreateScope();
            ICache second = injector.Get<ICache>();

            for (int i = 0; i < 5; i++)
                Assert.That(second.TryGetValue(key, out int _));
        }

        [Test]
        public void TryGetValue_ShouldResetTheExpiration() 
        {
            Assert.That(Cache!.Add(key, val, TimeSpan.FromSeconds(2)));
            Thread.Sleep(1200);
            Assert.That(Cache.TryGetValue(key, out int _));
            Thread.Sleep(1200);
            Assert.That(Cache.TryGetValue(key, out int _));
        }
    }

    [TestFixture]
    public class RedisCacheTests : CacheTestsBase<RedisCache> { }

    [TestFixture]
    public class MemoryCacheTests : CacheTestsBase<MemoryCache> { }
}
