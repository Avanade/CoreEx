#pragma warning disable IDE0130 // Namespace does not match folder structure; by design.
namespace OpenTelemetry.Trace;
#pragma warning restore IDE0130 // Namespace does not match folder structure

/// <summary>
/// Provides standard extensions.
/// </summary>
public static class CoreExCosmosExtensions
{
    /// <summary>
    /// Enables <i>CoreEx</i> OpenTelemetry instrumentation.
    /// </summary>
    /// <param name="builder">The <see cref="OpenTelemetryBuilder"/>.</param>
    /// <returns>The <paramref name="builder"/> to support fluent-style method-chaining.</returns>
    /// <remarks>Deliberately does <b>not</b> register <see cref="CoreEx.Cosmos.Extended.CosmosDbInvoker"/>/<see cref="CoreEx.Cosmos.Extended.CosmosDbUnitOfWorkInvoker"/> as tracing sources - both disable tracing
    /// themselves (<c>IsTracingDisabled</c>) since CRUD/unit-of-work operations are high-frequency; registering their (never-emitted) source would be dead weight. Only <see cref="CoreEx.Cosmos.Outbox.CosmosDbOutboxRelayInvoker"/>
    /// is registered, mirroring <c>WithCoreExPostgresTelemetry</c>/<c>WithCoreExSqlServerTelemetry</c>'s relay-only tracing choice.</remarks>
    public static OpenTelemetryBuilder WithCoreExCosmosDbTelemetry(this OpenTelemetryBuilder builder) => builder.ThrowIfNull()
        .WithCoreExEventsSources()  // Included here as they are leveraged by the Cosmos DB Outbox capabilities.
        .WithTracing(t => t.AddInvokerAsSource<CoreEx.Cosmos.Outbox.CosmosDbOutboxRelayInvoker>())
        .WithMetrics(m => m.AddMeter(CoreEx.Cosmos.CosmosMetrics.Meter.Name));
}
