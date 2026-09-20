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
    /// <remarks>On net8.0 targets, registering enough <see cref="TracerProviderBuilder.AddSource(string[])"/> patterns (including wildcards such as <c>"Azure.Messaging.ServiceBus.*"</c>, e.g. via
    /// <c>WithCoreExServiceBusTelemetry</c>) can cause OpenTelemetry's <c>WildcardHelper</c> to build a non-backtracking regex whose automata exceeds net8.0's default 1,000-node safe size (raised to 10,000 in
    /// .NET 9+; see dotnet/runtime <c>SymbolicRegexThresholds</c>), throwing <see cref="NotSupportedException"/> when the <c>TracerProvider</c> is constructed at host startup. This is not addressed here, since
    /// it would require globally raising the regex engine's automata-size cap for the whole process - a side effect this library should not impose on every regex in a consuming application. If a net8.0 host
    /// hits this exception, opt in explicitly by calling <see cref="IncreaseNet8RegexNonBacktrackingAutomataLimit"/> once, early in its own startup, before this method is invoked.</remarks>
    public static OpenTelemetryBuilder WithCoreExTelemetry(this OpenTelemetryBuilder builder)
    {
        builder.ThrowIfNull();

        return builder
            .WithTracing(t => t.AddHttpClientInstrumentation().WithCoreExSources())
            .WithMetrics(m => m.AddHttpClientInstrumentation().AddRuntimeInstrumentation().AddMeter("Polly"));
    }

    /// <summary>
    /// Raises the net8.0 non-backtracking regex engine's automata-size safety cap to <c>10,000</c> nodes for the current process - the same default net9.0+ already uses.
    /// </summary>
    /// <remarks>This mutates process-wide <see cref="AppContext"/> state (<c>AppContext.SetData("REGEX_NONBACKTRACKING_MAX_AUTOMATA_SIZE", 10_000)</c>), so it is deliberately <b>not</b> invoked automatically by
    /// <see cref="WithCoreExTelemetry"/>; call it explicitly and only if your host needs it - see the <see cref="WithCoreExTelemetry"/> remarks for when that is. Setting the equivalent
    /// <c>&lt;RuntimeHostConfigurationOption&gt;</c> MSBuild item in a <c>.csproj</c> does <b>not</b> work for this particular switch, as the regex engine reads it via <see cref="AppContext.GetData(string)"/>
    /// expecting a boxed <see cref="int"/>, not the <see cref="long"/>/<see cref="string"/> types the runtimeconfig.json bridge supplies; this method must be called in-process instead. This is a no-op on net9.0+,
    /// where the higher cap is already the default.</remarks>
    public static void IncreaseNet8RegexNonBacktrackingAutomataLimit() => AppContext.SetData("REGEX_NONBACKTRACKING_MAX_AUTOMATA_SIZE", 10_000);

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