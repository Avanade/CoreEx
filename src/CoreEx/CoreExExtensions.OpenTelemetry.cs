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
    /// <remarks>On net8.0 targets, registering enough <see cref="TracerProviderBuilder.AddSource(string[])"/> patterns (including wildcards such as <c>"Azure.Messaging.ServiceBus.*"</c>) can cause OpenTelemetry's
    /// <c>WildcardHelper</c> to build a non-backtracking regex whose automata exceeds net8.0's default 1,000-node safe size (raised to 10,000 in .NET 9+; see dotnet/runtime <c>SymbolicRegexThresholds</c>), throwing
    /// <see cref="NotSupportedException"/> when the <c>TracerProvider</c> is constructed at host startup. This is not addressed here, since it would require globally raising the regex engine's automata-size cap
    /// for the whole process via <c>AppContext.SetData("REGEX_NONBACKTRACKING_MAX_AUTOMATA_SIZE", ...)</c> - a side effect this library should not impose on every regex in a consuming application. If a net8.0 host
    /// hits this exception, opt in explicitly and locally by adding <c>&lt;RuntimeHostConfigurationOption Include="REGEX_NONBACKTRACKING_MAX_AUTOMATA_SIZE" Value="10000" /&gt;</c> to its own <c>.csproj</c>.</remarks>
    public static OpenTelemetryBuilder WithCoreExTelemetry(this OpenTelemetryBuilder builder)
    {
        builder.ThrowIfNull();

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