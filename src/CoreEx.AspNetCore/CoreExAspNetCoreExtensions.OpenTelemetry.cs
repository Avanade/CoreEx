#pragma warning disable IDE0130 // Namespace does not match folder structure; by design.
namespace OpenTelemetry.Trace;
#pragma warning restore IDE0130 // Namespace does not match folder structure

/// <summary>
/// Provides standard extensions.
/// </summary>
public static class CoreExAspNetCoreExtensions
{
    /// <summary>
    /// Enables <i>CoreEx</i> OpenTelemetry instrumentation for ASP.NET Core. <see cref="CoreEx.AspNetCore.Mvc.WebApi"/> and <see cref="CoreEx.AspNetCore.Http.WebApi"/>.
    /// </summary>
    /// <param name="builder">The <see cref="OpenTelemetryBuilder"/>.</param>
    /// <returns>The <paramref name="builder"/> to support fluent-style method-chaining.</returns>
    /// <remarks>Also, includes <see cref="WithCoreExAspNetCoreSources(TracerProviderBuilder)"/>.</remarks>
    public static OpenTelemetryBuilder WithAspNetCoreTelemetry(this OpenTelemetryBuilder builder)
        => builder.ThrowIfNull().ThrowIfNull()
            .WithTracing(t => t.AddAspNetCoreInstrumentation().WithCoreExAspNetCoreSources())
            .WithMetrics(m => m.AddAspNetCoreInstrumentation());

    /// <summary>
    /// Enables (adds) the <i>CoreEx</i>-specified OpenTelemetry tracing sources.
    /// </summary>
    /// <param name="builder">The <see cref="OpenTelemetryBuilder"/>.</param>
    /// <returns>The <paramref name="builder"/> to support fluent-style method-chaining.</returns>
    public static TracerProviderBuilder WithCoreExAspNetCoreSources(this TracerProviderBuilder builder) => builder.ThrowIfNull()
        .AddInvokerAsSource<CoreEx.AspNetCore.Http.WebApiInvoker>()
        .AddInvokerAsSource<CoreEx.AspNetCore.Mvc.WebApiInvoker>()
        .AddInvokerAsSource<CoreEx.AspNetCore.Idempotency.IdempotencyProviderInvoker>();
}