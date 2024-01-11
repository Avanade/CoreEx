using CoreEx.Caching;
using CoreEx.Results;
using NUnit.Framework;
using System.Threading.Tasks;

namespace CoreEx.Test.Framework.Caching
{
    [TestFixture]
    internal class ResultExtensionsTest
    {
        [Test]
        public async Task CacheGetOrAddAsync_PreExisting()
        {
            var rc = new RequestCache();
            rc.SetValue("a", 88);

            var r = await Result.Go().CacheGetOrAddAsync(rc, "a", () => Task.FromResult(Result.Go(99)));
            Assert.That(r.Value, Is.EqualTo(88));
        }

        [Test]
        public async Task CacheGetOrAddAsync_NotExisting()
        {
            var rc = new RequestCache();
            rc.SetValue("b", 88);

            var r = await Result.Go().CacheGetOrAddAsync(rc, "a", () => Task.FromResult(Result.Go(99)));
            Assert.That(r.Value, Is.EqualTo(99));
        }

        [Test]
        public async Task CacheGetOrAddAsync_PreError()
        {
            var rc = new RequestCache();
            rc.SetValue("a", 88);

            var r = await Result.Fail("bad").CacheGetOrAddAsync(rc, "a", () => Task.FromResult(Result.Go(99)));
            Assert.That(r.IsFailure, Is.True);
        }

        [Test]
        public async Task CacheGetOrAddAsync_FactoryError()
        {
            var rc = new RequestCache();
            rc.SetValue("b", 88);

            var r = await Result.Go().CacheGetOrAddAsync(rc, "a", () => Task.FromResult(Result<int>.Fail("bad")));
            Assert.That(r.IsFailure, Is.True);
        }
    }
}