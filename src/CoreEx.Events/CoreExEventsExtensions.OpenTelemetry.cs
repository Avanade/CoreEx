#pragma warning disable IDE0130 // Namespace does not match folder structure; by design.
namespace OpenTelemetry.Trace;
#pragma warning restore IDE0130 // Namespace does not match folder structure

/// <summary>
/// Provides standard extensions.
/// </summary>
public static class CoreExEventsExtensions
{
    /// <summary>
    /// Enables <i>CoreEx</i> OpenTelemetry instrumentation.
    /// </summary>
    /// <param name="builder">The <see cref="OpenTelemetryBuilder"/>.</param>
    /// <returns>The <paramref name="builder"/> to support fluent-style method-chaining.</returns>
    public static OpenTelemetryBuilder WithCoreExEventsSources(this OpenTelemetryBuilder builder) => builder.ThrowIfNull()
        .WithTracing(t => t
            .AddInvokerAsSource<CoreEx.Events.Publishing.EventPublisherInvoker>()
            .AddInvokerAsSource<CoreEx.Events.Subscribing.SubscribedInvoker>())
        .WithMetrics(m => m
            .AddMeter(CoreEx.Events.Subscribing.EventSubscriberMetrics.Meter.Name));
}