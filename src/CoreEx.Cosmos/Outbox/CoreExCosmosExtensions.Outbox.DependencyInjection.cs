#pragma warning disable IDE0130 // Namespace does not match folder structure; by design.
namespace Microsoft.Extensions.Hosting;
#pragma warning restore IDE0130 // Namespace does not match folder structure

/// <summary>
/// Provides <see cref="CosmosDbOutboxRelay"/> and related extensions.
/// </summary>
public static class CoreExCosmosOutboxExtensions
{
    /// <summary>
    /// Adds <b>singleton</b> <see cref="CosmosDbOutboxRelayHostedService"/> keyed service(s) (as per <paramref name="servicesCount"/>) that will be executed as a hosted service (in the background), relaying
    /// outbox event documents from the specified <paramref name="containerId"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder"/>.</param>
    /// <param name="containerId">The monitored <see cref="Container"/> identifier - the container whose outbox event documents (written by a <see cref="CosmosDbEventPublisher"/> against the same container) are
    /// to be relayed. Required, unlike the equivalent SQL Server/Postgres extensions, since Cosmos DB outbox documents are co-located per-container rather than centralized in one table.</param>
    /// <param name="servicesCount">The number of hosted services to start to enable concurrency of processing for this <paramref name="containerId"/> (each gets its own Change Feed Processor instance, sharing
    /// the same lease container for coordination). Where not specified, attempts to get the value from configuration using '<c>CoreEx:Host:Services:CosmosOutboxRelay:{containerId}:ServicesCount</c>' as the
    /// key (namespaced by <paramref name="containerId"/> so different containers can have independent concurrency); otherwise, defaults to '<c>4</c>'.</param>
    /// <param name="leaseContainerId">The lease <see cref="Container"/> identifier; where not specified, defaults to <c>"{containerId}-leases"</c>.</param>
    /// <param name="serviceKeyPrefix">The keyed singleton and health check key prefix, and the basis for each instance's distinct Change Feed Processor instance name; where not specified, defaults to
    /// <c>"cosmos-outbox-relay-{containerId}-"</c>.</param>
    /// <param name="configureOptions">An optional action to configure each <see cref="CosmosDbOutboxRelayOptions"/> instance before its <see cref="CosmosDbOutboxRelay"/> is built.</param>
    /// <param name="configure">An optional action to configure each <see cref="CosmosDbOutboxRelayHostedService"/> instance.</param>
    /// <returns>The <see cref="IHostApplicationBuilder"/> for fluent-style method-chaining.</returns>
    /// <remarks>Uses the <see cref="Microsoft.Extensions.DependencyInjection.CoreExExtensions.AddHostedService{THostedService}(IServiceCollection, string, Func{IServiceProvider, THostedService}, Action{IServiceProvider, THostedService}?)"/>
    /// to enable, matching the same pattern as the SQL Server/Postgres outbox relay registrations.
    /// <para>Call this once per container that hosts outbox documents, each with its own <paramref name="servicesCount"/> (e.g. more instances for a high-volume "orders" container, fewer for a low-volume
    /// "customers" one).</para></remarks>
    public static IHostApplicationBuilder AddCosmosDbOutboxRelayHostedService(this IHostApplicationBuilder builder, string containerId, int? servicesCount = null, string? leaseContainerId = null, string? serviceKeyPrefix = null,
        Action<IServiceProvider, CosmosDbOutboxRelayOptions>? configureOptions = null, Action<IServiceProvider, CosmosDbOutboxRelayHostedService>? configure = null)
    {
        builder.ThrowIfNull();
        containerId.ThrowIfNullOrEmpty();

        servicesCount ??= CoreEx.Abstractions.Internal.GetConfigurationValue<int>($"CoreEx:Host:Services:CosmosOutboxRelay:{containerId}:ServicesCount", 4, builder.Configuration);
        servicesCount.ThrowWhen(c => c <= 0 || c > 32);

        leaseContainerId ??= $"{containerId}-leases";
        serviceKeyPrefix ??= $"cosmos-outbox-relay-{containerId}-";

        for (var i = 0; i < servicesCount; i++)
        {
            var instanceName = $"{serviceKeyPrefix}{i:00}";
            builder.Services.AddHostedService<CosmosDbOutboxRelayHostedService>(instanceName, sp =>
            {
                var options = new CosmosDbOutboxRelayOptions { ContainerId = containerId, LeaseContainerId = leaseContainerId, InstanceName = instanceName };
                configureOptions?.Invoke(sp, options);

                // A short-lived scope, used only to obtain the reusable Microsoft.Azure.Cosmos.Database SDK proxy - ICosmosDb itself is registered scoped, but CosmosDbOutboxRelay is built once and lives for
                // the process lifetime, so it cannot capture a scoped ICosmosDb directly (a captive-dependency bug); Database, like Container/CosmosClient, is stable and safe to hold long-term.
                using var scope = sp.CreateScope();
                var database = scope.ServiceProvider.GetRequiredService<ICosmosDb>().Database;

                var processor = ActivatorUtilities.CreateInstance<CosmosDbOutboxRelayProcessor>(sp, containerId);
                var relay = ActivatorUtilities.CreateInstance<CosmosDbOutboxRelay>(sp, database, options, processor);
                return ActivatorUtilities.CreateInstance<CosmosDbOutboxRelayHostedService>(sp, relay);
            }, configure);
        }

        return builder;
    }
}
