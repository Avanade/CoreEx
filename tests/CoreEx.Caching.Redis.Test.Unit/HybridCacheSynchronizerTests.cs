using CoreEx.Hosting.Synchronization;
using Microsoft.Extensions.DependencyInjection;

namespace CoreEx.Caching.Redis.Test.Unit;

public class HybridCacheSynchronizerTests : WithGenericTester<EntryPoint>
{
    [Test]
    public async Task Synchronize_Two_Processes()
    {
        // Arrange.
        var root = Test.Services.GetRequiredService<IServiceProvider>();

        await using var ss1 = root.CreateAsyncScope();
        var sp1 = ss1.ServiceProvider;
        await using var hs1 = sp1.GetRequiredService<HybridCacheSynchronizer>();

        await using var ss2 = root.CreateAsyncScope();
        var sp2 = ss2.ServiceProvider;
        await using var hs2 = sp2.GetRequiredService<HybridCacheSynchronizer>();

        // Act/Assert - First should enter, second should not.
        var r1 = await hs1.EnterAsync<HybridCacheSynchronizerTests>();
        r1.Should().BeTrue();

        var r2 = await hs2.EnterAsync<HybridCacheSynchronizerTests>();
        r2.Should().BeFalse();

        // Assert - After first exits, second can enter.
        await hs1.ExitAsync<HybridCacheSynchronizerTests>();

        r2 = await hs2.EnterAsync<HybridCacheSynchronizerTests>();
        r2.Should().BeTrue();

        await hs2.ExitAsync<HybridCacheSynchronizerTests>();
    }
}