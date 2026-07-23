namespace CoreEx.Data.GraphQL;

/// <summary>
/// Defines a GraphQL-lite query execution engine that bridges a GraphQL document to the underlying <see cref="QueryArgs"/>/<see cref="PagingArgs"/> dynamic querying capability.
/// </summary>
/// <remarks>This is intentionally transport-agnostic (no dependency on ASP.NET Core or any specific GraphQL parsing library) so that hosting bridges (e.g. minimal API endpoints) can depend on this
/// contract alone. See <c>CoreEx.Data.GraphQL</c> for the concrete implementation, and <c>CoreEx.AspNetCore</c> for the corresponding minimal API hosting bridge.
/// <para>This is <b>not</b> intended to be a full GraphQL implementation; there is no support for mutations, subscriptions, fragments, directives, interfaces or unions, nor cross-repository nested
/// object-graph resolution (no dataloader/N+1 batching). It supports read-only root field queries whose selection sets project fields already present on the single object graph returned by one
/// underlying query/get operation.</para></remarks>
public interface IGraphQLEngine
{
    /// <summary>
    /// Executes the specified GraphQL <paramref name="document"/> against the registered query roots.
    /// </summary>
    /// <param name="document">The GraphQL query document.</param>
    /// <param name="operationName">The optional operation name (required where <paramref name="document"/> contains multiple named operations).</param>
    /// <param name="variables">The optional GraphQL variables.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="GraphQLEngineResult"/>.</returns>
    Task<GraphQLEngineResult> ExecuteAsync(string document, string? operationName = null, IReadOnlyDictionary<string, object?>? variables = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the discovery/schema document describing the registered query roots (supported filter/orderby fields and selectable output fields per root).
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The schema as a <see cref="JsonElement"/>.</returns>
    Task<JsonElement> GetSchemaAsync(CancellationToken cancellationToken = default);
}
