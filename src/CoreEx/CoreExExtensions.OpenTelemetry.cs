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

#if !NET9_0_OR_GREATER
        // .NET 8's non-backtracking regex engine defaults to a 1,000-node safe automata size (raised to 10,000 in .NET 9+; see dotnet/runtime SymbolicRegexThresholds). OpenTelemetry's
        // WildcardHelper builds a NonBacktracking regex from every registered ActivitySource name/pattern (including wildcards such as "Azure.Messaging.ServiceBus.*"), and with enough
        // registered sources the resulting automata can exceed net8.0's lower cap, throwing NotSupportedException when the TracerProvider is constructed at host startup. Align net8.0
        // with the .NET 9+ default so this does not fail solely due to running on an older target framework.
        AppContext.SetData("REGEX_NONBACKTRACKING_MAX_AUTOMATA_SIZE", 10_000);
#endif

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