namespace Contoso.Customers.Relay;

/// <summary>
/// Configures instrumentation for the relay's shared Cosmos client.
/// </summary>
public static class CosmosClientInstrumentationExtensions
{
    /// <summary>
    /// Suppresses SDK background work started during client construction without suppressing subsequent foreground operations.
    /// </summary>
    /// <param name="services">The services containing Aspire's singleton Cosmos client factory.</param>
    public static void SuppressCosmosClientBackgroundInstrumentation(this IServiceCollection services)
    {
        var registration = services.LastOrDefault(s => s.ServiceType == typeof(CosmosClient) && !s.IsKeyedService)
            ?? throw new InvalidOperationException("Register the Cosmos client before configuring background instrumentation.");
        var factory = registration.ImplementationFactory
            ?? throw new InvalidOperationException("The Cosmos client must be registered using a factory before configuring background instrumentation.");
        if (registration.Lifetime != ServiceLifetime.Singleton)
            throw new InvalidOperationException("The Cosmos client must be registered as a singleton.");

        // Account refresh starts at client construction, before the change-feed processor's suppression scope.
        services[services.IndexOf(registration)] = ServiceDescriptor.Singleton<CosmosClient>(sp =>
        {
            using var instrumentation = SuppressInstrumentationScope.Begin();
            return (CosmosClient)factory(sp);
        });
    }
}
