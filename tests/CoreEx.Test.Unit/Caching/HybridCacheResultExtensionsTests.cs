using CoreEx.Caching;
using CoreEx.Entities;
using CoreEx.Results;

namespace CoreEx.Test.Unit.Caching;

[TestFixture]
public class HybridCacheResultExtensionsTests
{
    private class Widget : IIdentifier<string?>
    {
        public string? Id { get; set; }
    }

    private static readonly HybridCacheEntryOptions _options = new() { Strategy = CacheStrategy.Local };

    [Test]
    public async Task GetOrCreateWithResultAsync_Success_IsCachedAndFactoryNotReinvoked()
    {
        var cache = new MemoryOnlyHybridCache();
        var count = 0;
        Task<Result<Widget>> Factory(CancellationToken ct) { count++; return Task.FromResult<Result<Widget>>(new Widget { Id = "w1" }); }

        var r1 = await cache.GetOrCreateWithResultAsync<Widget>("w1", Factory, _options);
        var r2 = await cache.GetOrCreateWithResultAsync<Widget>("w1", Factory, _options);

        r1.IsSuccess.Should().BeTrue();
        r2.IsSuccess.Should().BeTrue();
        r2.Value.Id.Should().Be("w1");
        count.Should().Be(1);
    }

    [Test]
    public async Task GetOrCreateWithResultAsync_Failure_IsReturnedAndNeverCached()
    {
        var cache = new MemoryOnlyHybridCache();
        var count = 0;
        Task<Result<Widget>> Factory(CancellationToken ct) { count++; return Task.FromResult<Result<Widget>>(count == 1 ? Result.NotFoundError() : new Widget { Id = "w2" }); }

        var r1 = await cache.GetOrCreateWithResultAsync<Widget>("w2", Factory, _options);
        r1.IsNotFoundError.Should().BeTrue();

        var r2 = await cache.GetOrCreateWithResultAsync<Widget>("w2", Factory, _options);
        r2.IsSuccess.Should().BeTrue();
        count.Should().Be(2);

        // Now cached; the factory is not invoked again.
        var r3 = await cache.GetOrCreateWithResultAsync<Widget>("w2", Factory, _options);
        r3.IsSuccess.Should().BeTrue();
        count.Should().Be(2);
    }

    [Test]
    public async Task GetOrCreateByKeyWithResultAsync_Success_IsCached()
    {
        IHybridCache cache = new MemoryOnlyHybridCache();
        var count = 0;
        Task<Result<string>> Factory(CancellationToken ct) { count++; return Task.FromResult<Result<string>>("value"); }

        (await cache.GetOrCreateByKeyWithResultAsync("k1", Factory, _options)).Value.Should().Be("value");
        (await cache.GetOrCreateByKeyWithResultAsync("k1", Factory, _options)).Value.Should().Be("value");
        count.Should().Be(1);
    }
}
