using CoreEx.Caching;
using CoreEx.Entities;
using NUnit.Framework;

namespace CoreEx.Test.Framework.Caching
{
    [TestFixture]
    public class RequestCacheTest
    {
        [Test]
        public void CacheKeyEntityKeyPrecedence()
        {
            var e = new Entity();

            var rc = new RequestCache();
            rc.SetValue(e);

            Assert.That(rc.TryGetValue(new CompositeKey(1), out Entity? value), Is.False);
            Assert.That(rc.TryGetValue(new CompositeKey(2), out value), Is.True);
        }
    }

    public class Entity : IEntityKey, ICacheKey
    {
        public CompositeKey EntityKey => new(1);
        public CompositeKey CacheKey => new(2);
    }
}