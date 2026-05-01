#pragma warning disable IDE0130 // Namespace does not match folder structure; by design.
namespace OpenTelemetry.Trace;
#pragma warning restore IDE0130 // Namespace does not match folder structure

/// <summary>
/// Provides standard extensions.
/// </summary>
public static class CoreExSqlServerExtensions
{
    /// <summary>
    /// Enables <i>CoreEx</i> OpenTelemetry instrumentation.
    /// </summary>
    /// <param name="builder">The <see cref="OpenTelemetryBuilder"/>.</param>
    /// <returns>The <paramref name="builder"/> to support fluent-style method-chaining.</returns>
    public static OpenTelemetryBuilder WithCoreExSqlServerTelemetry(this OpenTelemetryBuilder builder) => builder.ThrowIfNull()
        .WithCoreExEventsSources()  // Included here as they are leveraged by the Sql Server Outbox capabilities.
        .WithTracing(t => t.AddInvokerAsSource<CoreEx.Database.SqlServer.Extended.SqlServerInvoker>()
                           .AddInvokerAsSource<CoreEx.Database.SqlServer.Extended.SqlServerUnitOfWorkInvoker>()
                           .AddSource("CoreEx.Database.Outbox.Relay"))
        .WithMetrics(m => m.AddMeter(SqlServerMetrics.Meter.Name));
}