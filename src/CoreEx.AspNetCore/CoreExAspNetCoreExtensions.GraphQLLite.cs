#pragma warning disable IDE0130 // Namespace does not match folder structure; by design.
namespace Microsoft.AspNetCore.Builder;
#pragma warning restore IDE0130 // Namespace does not match folder structure

/// <summary>
/// Provides the <see cref="MapCoreExGraphQLLite(IEndpointRouteBuilder, string, Action{IEndpointConventionBuilder}?)"/> hosting bridge for the CoreEx GraphQL-lite <see cref="IGraphQLEngine"/>.
/// </summary>
public static partial class CoreExAspNetCoreExtensions
{
    /// <summary>
    /// Maps a minimal API POST endpoint at <paramref name="pattern"/> that bridges the standard GraphQL-over-HTTP request envelope (<c>query</c>/<c>operationName</c>/<c>variables</c>) to the
    /// registered <see cref="IGraphQLEngine"/>, resolved from dependency injection.
    /// </summary>
    /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/>.</param>
    /// <param name="pattern">The route pattern; defaults to <c>/query</c>.</param>
    /// <param name="configure">An optional <see cref="Action{T}"/> to configure the <see cref="IEndpointConventionBuilder"/>.</param>
    /// <returns>The <paramref name="endpoints"/> to support fluent-style method-chaining.</returns>
    /// <remarks>This is intentionally a thin, transport-only bridge: all parsing, argument mapping, field-selection projection, and error mapping is performed by the registered <see cref="IGraphQLEngine"/>
    /// implementation (see the <c>CoreEx.Data.GraphQL</c> package). Register the engine and its query/item roots via <c>services.AddCoreExGraphQLLite((o, sp) =&gt; o.AddQuery(...))</c> before calling this
    /// method. The endpoint is additive to any existing REST controllers/endpoints for the same domain — it is not a replacement.</remarks>
    public static IEndpointRouteBuilder MapCoreExGraphQLLite(this IEndpointRouteBuilder endpoints, string pattern = "/query", Action<IEndpointConventionBuilder>? configure = null)
    {
        endpoints.ThrowIfNull();
        pattern.ThrowIfNullOrEmpty();

        var rb = endpoints.MapPost(pattern, (HttpRequest request, CoreEx.AspNetCore.Http.WebApi webApi, IGraphQLEngine engine, CancellationToken cancellationToken)
            => webApi.PostAsync<GraphQLLiteResponse>(request, (ro, ct) => ExecuteGraphQLLiteAsync(request, webApi.JsonSerializerOptions, engine, ct), statusCode: HttpStatusCode.OK, cancellationToken: cancellationToken))
            .WithName($"CoreExGraphQLLite{pattern.Replace('/', '_')}")
            .WithDisplayName("GraphQL Lite")
            .WithTags("GraphQL");

        configure?.Invoke(rb);

        return endpoints;
    }

    /// <summary>
    /// Executes the GraphQL-lite request.
    /// </summary>
    private static async Task<GraphQLLiteResponse> ExecuteGraphQLLiteAsync(HttpRequest request, JsonSerializerOptions jso, IGraphQLEngine engine, CancellationToken cancellationToken)
    {
        GraphQLLiteRequest? body;
        try
        {
            // Self -deserializing the request body to the standard GraphQL-over-HTTP request envelope and self-handling any deserialization errors to return a GraphQL-lite error response.
            body = await JsonSerializer.DeserializeAsync<GraphQLLiteRequest>(request.Body, jso, cancellationToken).ConfigureAwait(false);
        }
        catch (JsonException ex)
        {
            return new GraphQLLiteResponse(null, [new GraphQLEngineError($"The request body is not a valid GraphQL-over-HTTP request: {ex.Message}")]);
        }

        if (body is null || string.IsNullOrEmpty(body.Query))
            return new GraphQLLiteResponse(null, [new GraphQLEngineError("The 'query' field is required.")]);

        var result = await engine.ExecuteAsync(body.Query, body.OperationName, body.Variables, cancellationToken).ConfigureAwait(false);
        return new GraphQLLiteResponse(result.Data, result.Errors);
    }
}

/// <summary>
/// Represents the standard GraphQL-over-HTTP POST request envelope.
/// </summary>
/// <param name="Query">The GraphQL document.</param>
/// <param name="OperationName">The optional operation name (required only where <paramref name="Query"/> contains multiple operations).</param>
/// <param name="Variables">The optional GraphQL variables.</param>
internal sealed record GraphQLLiteRequest(string? Query, string? OperationName, IReadOnlyDictionary<string, object?>? Variables);

/// <summary>
/// Represents the standard GraphQL-over-HTTP response envelope.
/// </summary>
/// <param name="Data">The result data.</param>
/// <param name="Errors">The result errors.</param>
internal sealed record GraphQLLiteResponse(JsonElement? Data, IReadOnlyList<GraphQLEngineError>? Errors);
