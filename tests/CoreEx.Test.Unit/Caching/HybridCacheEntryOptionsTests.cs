using CoreEx.Caching;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CoreEx.Test.Unit.Caching;

[TestFixture]
public class HybridCacheEntryOptionsTests
{
    private class Widget { }

    [TearDown]
    public void TearDown() => ExecutionContext.Reset();

    [Test]
    public void CreateFor_UsesActualTypeName_NotLiteralGenericParameterName()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "CoreEx:Caching:Widget:LocalExpiration", "00:10:00" },
                { "CoreEx:Caching:T:LocalExpiration", "00:20:00" } // Decoy: must NOT be picked up.
            })
            .Build();

        var sc = new ServiceCollection();
        sc.AddSingleton<IConfiguration>(config);
        using var sp = sc.BuildServiceProvider();

        ExecutionContext.SetCurrent(new ExecutionContext { ServiceProvider = sp });

        var options = HybridCacheEntryOptions.CreateFor<Widget>();

        options.LocalExpiration.Should().Be(TimeSpan.FromMinutes(10));
    }

    [Test]
    public void CreateFor_DifferentTypes_ResolveDistinctConfiguration()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "CoreEx:Caching:Widget:LocalExpiration", "00:10:00" },
                { "CoreEx:Caching:Gadget:LocalExpiration", "00:15:00" }
            })
            .Build();

        var sc = new ServiceCollection();
        sc.AddSingleton<IConfiguration>(config);
        using var sp = sc.BuildServiceProvider();

        ExecutionContext.SetCurrent(new ExecutionContext { ServiceProvider = sp });

        HybridCacheEntryOptions.CreateFor<Widget>().LocalExpiration.Should().Be(TimeSpan.FromMinutes(10));
        HybridCacheEntryOptions.CreateFor<Gadget>().LocalExpiration.Should().Be(TimeSpan.FromMinutes(15));
    }

    private class Gadget { }
}
