using CoreEx.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace CoreEx.Test.Unit.DependencyInjection;

[TestFixture]
public class CoreExExtensionsDependencyInjectionTests
{
    [ScopedService]
    private class ScopedImpl { }

    [SingletonService]
    private class SingletonImpl { }

    [TransientService]
    private class TransientImpl { }

    [Test]
    public void AddDynamicServicesUsing_RegistersDecoratedTypesWithCorrectLifetimes()
    {
        var services = new ServiceCollection();
        services.AddDynamicServicesUsing(typeof(CoreExExtensionsDependencyInjectionTests).Assembly);

        services.Should().Contain(sd => sd.ServiceType == typeof(ScopedImpl) && sd.Lifetime == ServiceLifetime.Scoped);
        services.Should().Contain(sd => sd.ServiceType == typeof(SingletonImpl) && sd.Lifetime == ServiceLifetime.Singleton);
        services.Should().Contain(sd => sd.ServiceType == typeof(TransientImpl) && sd.Lifetime == ServiceLifetime.Transient);
    }

    [Test]
    public void AddDynamicServicesUsing_GenericAssemblyOverload_MatchesExplicitAssembly()
    {
        var services = new ServiceCollection();
        services.AddDynamicServicesUsing<CoreExExtensionsDependencyInjectionTests>();

        services.Should().Contain(sd => sd.ServiceType == typeof(ScopedImpl) && sd.Lifetime == ServiceLifetime.Scoped);
    }
}
