using CoreEx.RefData.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace CoreEx.RefData.Test.Unit;

public class CoreExReferenceDataExtensionsTests
{
    private class DummyProvider : IReferenceDataProvider
    {
        public IEnumerable<(Type, Type)> Types => [];

        public Task<IReferenceDataCollection> GetAsync(Type type, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private static ServiceCollection CreateBaseServices()
    {
        var sc = new ServiceCollection();
        sc.AddSingleton(Mock.Of<ILogger<ReferenceDataOrchestrator>>());
        return sc;
    }

    private static ReferenceDataOrchestrator DefaultFactory(IServiceProvider sp) => new(sp, sp.GetRequiredService<ILogger<ReferenceDataOrchestrator>>());

    [Test]
    public void WithFactory_RegistersSingleton()
    {
        var sc = CreateBaseServices();
        sc.AddReferenceDataOrchestrator(DefaultFactory);
        using var sp = sc.BuildServiceProvider();

        var o1 = sp.GetRequiredService<ReferenceDataOrchestrator>();
        var o2 = sp.GetRequiredService<ReferenceDataOrchestrator>();
        o1.Should().BeSameAs(o2);
    }

    [Test]
    public void FactoryReturnsNull_ThrowsOnResolve()
    {
        var sc = CreateBaseServices();
        sc.AddReferenceDataOrchestrator(_ => null!);
        using var sp = sc.BuildServiceProvider();

        Action act = () => sp.GetRequiredService<ReferenceDataOrchestrator>();
        act.Should().Throw<InvalidOperationException>().WithMessage("*factory returned a null*");
    }

    [Test]
    public void AutoRegistersDefaultQuery_WhenNoneRegistered()
    {
        var sc = CreateBaseServices();
        sc.AddReferenceDataOrchestrator(DefaultFactory);
        using var sp = sc.BuildServiceProvider();

        var orch = sp.GetRequiredService<ReferenceDataOrchestrator>();
        orch.HasRegisteredQuery.Should().BeTrue();
    }

    [Test]
    public void DoesNotOverrideExistingQuery()
    {
        var sc = CreateBaseServices();
        var customQuery = new ReferenceDataQuery();
        sc.AddReferenceDataOrchestrator(sp => DefaultFactory(sp).RegisterQuery(customQuery));
        using var sp = sc.BuildServiceProvider();

        var orch = sp.GetRequiredService<ReferenceDataOrchestrator>();
        orch.HasRegisteredQuery.Should().BeTrue();
    }

    [Test]
    public void HealthCheckTrue_RegistersHealthCheck()
    {
        var sc = CreateBaseServices();
        sc.AddReferenceDataOrchestrator(DefaultFactory, healthCheck: true);
        using var sp = sc.BuildServiceProvider();

        var options = sp.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;
        options.Registrations.Should().Contain(r => r.Name == "reference-data-orchestrator");
    }

    [Test]
    public void HealthCheckFalse_DoesNotRegisterHealthCheck()
    {
        var sc = CreateBaseServices();
        sc.AddReferenceDataOrchestrator(DefaultFactory, healthCheck: false);
        using var sp = sc.BuildServiceProvider();

        var options = sp.GetService<IOptions<HealthCheckServiceOptions>>();
        (options?.Value.Registrations.Any(r => r.Name == "reference-data-orchestrator") ?? false).Should().BeFalse();
    }

    [Test]
    public void CustomHealthCheckName_IsUsed()
    {
        var sc = CreateBaseServices();
        sc.AddReferenceDataOrchestrator(DefaultFactory, healthCheckName: "custom-name");
        using var sp = sc.BuildServiceProvider();

        var options = sp.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;
        options.Registrations.Should().Contain(r => r.Name == "custom-name");
    }

    [Test]
    public void NoHybridCacheRegistered_FallsBackToMemoryOnly()
    {
        var sc = CreateBaseServices();
        sc.AddReferenceDataOrchestrator(DefaultFactory);
        using var sp = sc.BuildServiceProvider();
        using var scope = sp.CreateScope();

        var cache = scope.ServiceProvider.GetRequiredService<IReferenceDataCache>();
        cache.Should().BeOfType<ReferenceDataHybridCache>();
        ((ReferenceDataHybridCache)cache).Cache.Should().BeOfType<Caching.MemoryOnlyHybridCache>();
    }

    [Test]
    public void ExistingReferenceDataCache_IsNotOverridden()
    {
        var sc = CreateBaseServices();
        var customCache = new ReferenceDataHybridCache(new Caching.MemoryOnlyHybridCache());
        sc.AddScoped<IReferenceDataCache>(_ => customCache);
        sc.AddReferenceDataOrchestrator(DefaultFactory);
        using var sp = sc.BuildServiceProvider();
        using var scope = sp.CreateScope();

        var cache = scope.ServiceProvider.GetRequiredService<IReferenceDataCache>();
        cache.Should().BeSameAs(customCache);
    }

    [Test]
    public void GenericProviderOverload_UsesRegisteredProvider()
    {
        var sc = CreateBaseServices();
        sc.AddScoped<DummyProvider>();
        sc.AddReferenceDataOrchestrator<DummyProvider>();
        using var sp = sc.BuildServiceProvider();

        sp.GetRequiredService<ReferenceDataOrchestrator>().Should().NotBeNull();
    }

    [Test]
    public void NoProviderOverload_UsesRegisteredIReferenceDataProvider()
    {
        var sc = CreateBaseServices();
        sc.AddScoped<IReferenceDataProvider, DummyProvider>();
        sc.AddReferenceDataOrchestrator();
        using var sp = sc.BuildServiceProvider();

        sp.GetRequiredService<ReferenceDataOrchestrator>().Should().NotBeNull();
    }
}
