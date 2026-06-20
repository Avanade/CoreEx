#pragma warning disable IDE0130 // Namespace does not match folder structure; by design.
namespace OpenTelemetry.Trace;
#pragma warning restore IDE0130 // Namespace does not match folder structure

/// <summary>
/// Provides standard extensions.
/// </summary>
public static class CoreExExtensions
{
    /// <summary>
    /// Enables <i>CoreEx</i> OpenTelemetry instrumentation (including <see cref="WithCoreExSources(TracerProviderBuilder)"/>).
    /// </summary>
    /// <param name="builder">The <see cref="OpenTelemetryBuilder"/>.</param>
    /// <returns>The <paramref name="builder"/> to support fluent-style method-chaining.</returns>
    public static OpenTelemetryBuilder WithCoreExTelemetry(this OpenTelemetryBuilder builder)
        => builder.ThrowIfNull().ThrowIfNull()
            .WithTracing(t => t.AddHttpClientInstrumentation().WithCoreExSources())
            .WithMetrics(m => m.AddHttpClientInstrumentation().AddRuntimeInstrumentation().AddProcessInstrumentation().AddMeter("Polly"));

    /// <summary>
    /// Enables (adds) the <i>CoreEx</i>-specified OpenTelemetry tracing sources.
    /// </summary>
    /// <param name="builder">The <see cref="OpenTelemetryBuilder"/>.</param>
    /// <returns>The <paramref name="builder"/> to support fluent-style method-chaining.</returns>
    public static TracerProviderBuilder WithCoreExSources(this TracerProviderBuilder builder) => builder.ThrowIfNull()
        .AddInvokerAsSource<CoreEx.Hosting.Work.WorkOrchestratorInvoker>()
        .AddInvokerAsSource<CoreEx.Hosting.HostedServiceInvoker>()
        .AddInvokerAsSource<CoreEx.RefData.ReferenceDataOrchestratorInvoker>();

    /// <summary>
    /// Adds the <typeparamref name="TInvoker"/> as the <see cref="TracerProviderBuilder.AddSource(string[])"/> using the <see cref="InvokerNameAttribute.GetName{T}()"/>.
    /// </summary>
    /// <typeparam name="TInvoker">The <see cref="InvokerBase"/> <see cref="Type"/>.</typeparam>
    /// <param name="builder">The <see cref="TracerProviderBuilder"/>.</param>
    /// <returns>The <paramref name="builder"/> to support fluent-style method-chaining.</returns>
    public static TracerProviderBuilder AddInvokerAsSource<TInvoker>(this TracerProviderBuilder builder) where TInvoker : InvokerBase
        => builder.ThrowIfNull().AddSource(InvokerNameAttribute.GetName<TInvoker>());
}