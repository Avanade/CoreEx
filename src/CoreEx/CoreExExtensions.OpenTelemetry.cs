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
    {
        builder.ThrowIfNull();

        builder.Services.AddOptions<OpenTelemetry.Instrumentation.Http.HttpClientTraceInstrumentationOptions>().Configure<IServiceProvider>((options, services) =>
        {
            var enrich = options.EnrichWithHttpRequestMessage;
            options.EnrichWithHttpRequestMessage = (activity, request) =>
            {
                enrich?.Invoke(activity, request);
                if (!Correlation.IsEnabled(services.GetService<IConfiguration>()))
                    return;

                var identifiers = request.Headers.TryGetValues(HttpNames.CorrelationIdHeaderName, out var values) ? values.ToArray() : [];
                var value = identifiers.Length == 1 ? identifiers[0] : null;
                activity.SetTag(Correlation.ActivityTagName, Correlation.IsValid(value) ? value : null);
            };
        });

        return builder
            .WithTracing(t => t.AddHttpClientInstrumentation().WithCoreExSources())
            .WithMetrics(m => m.AddHttpClientInstrumentation().AddRuntimeInstrumentation().AddMeter("Polly"));
    }

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
