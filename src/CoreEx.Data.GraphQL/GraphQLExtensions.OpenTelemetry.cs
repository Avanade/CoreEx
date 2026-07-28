#pragma warning disable IDE0130 // Namespace does not match folder structure; by design.
namespace OpenTelemetry.Trace;
#pragma warning restore IDE0130 // Namespace does not match folder structure

/// <summary>
/// Provides standard extensions.
/// </summary>
public static class GraphQLExtensions
{
    /// <summary>
    /// Enables <i>CoreEx.Data.GraphQL</i> OpenTelemetry instrumentation.
    /// </summary>
    /// <param name="builder">The <see cref="OpenTelemetryBuilder"/>.</param>
    /// <returns>The <paramref name="builder"/> to support fluent-style method-chaining.</returns>
    public static OpenTelemetryBuilder WithCoreExGraphQLTelemetry(this OpenTelemetryBuilder builder) => builder.ThrowIfNull()
        .WithCoreExEventsSources()
        .WithTracing(t => t.AddInvokerAsSource<CoreEx.Data.GraphQL.GraphQLEngineInvoker>());
}
