#pragma warning disable IDE0130 // Namespace does not match folder structure; by design.
namespace Microsoft.AspNetCore.Builder;
#pragma warning restore IDE0130 // Namespace does not match folder structure

/// <summary>
/// Provides the <see cref="MapCoreExGraphQLLite(IEndpointRouteBuilder, string)"/> hosting bridge for the CoreEx GraphQL-lite <see cref="IGraphQLEngine"/>.
/// </summary>
public static partial class CoreExAspNetCoreExtensions
{
    /// <summary>
    /// Maps a minimal API POST endpoint at <paramref name="pattern"/> that bridges the standard GraphQL-over-HTTP request envelope (<c>query</c>/<c>operationName</c>/<c>variables</c>) to the
    /// registered <see cref="IGraphQLEngine"/>, resolved from dependency injection.
    /// </summary>
    /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/>.</param>
    /// <param name="pattern">The route pattern; defaults to <c>/query</c>.</param>
    /// <returns>The <paramref name="endpoints"/> to support fluent-style method-chaining.</returns>
    /// <remarks>This is intentionally a thin, transport-only bridge: all parsing, argument mapping, field-selection projection, and error mapping is performed by the registered <see cref="IGraphQLEngine"/>
    /// implementation (see the <c>CoreEx.Data.GraphQL</c> package). Register the engine and its query/item roots via <c>services.AddCoreExGraphQLLite(o =&gt; o.AddQuery(...))</c> before calling this
    /// method. The endpoint is additive to any existing REST controllers/endpoints for the same domain — it is not a replacement.</remarks>
    public static IEndpointRouteBuilder MapCoreExGraphQLLite(this IEndpointRouteBuilder endpoints, string pattern = "/query")
    {
        endpoints.ThrowIfNull();
        pattern.ThrowIfNullOrEmpty();

        endpoints.MapPost(pattern, async (HttpRequest request, HttpResponse response, IGraphQLEngine engine, CancellationToken cancellationToken) =>
        {
            GraphQLLiteRequest? body;
            try
            {
                body = await JsonSerializer.DeserializeAsync<GraphQLLiteRequest>(request.Body, JsonDefaults.SerializerOptions, cancellationToken).ConfigureAwait(false);
            }
            catch (JsonException ex)
            {
                await Results.Json(new GraphQLLiteResponse(null, [new GraphQLEngineError($"The request body is not a valid GraphQL-over-HTTP request: {ex.Message}")]),
                    JsonDefaults.SerializerOptions, statusCode: (int)HttpStatusCode.BadRequest).ExecuteAsync(request.HttpContext).ConfigureAwait(false);

                return;
            }

            if (body is null || string.IsNullOrEmpty(body.Query))
            {
                await Results.Json(new GraphQLLiteResponse(null, [new GraphQLEngineError("The 'query' field is required.")]), JsonDefaults.SerializerOptions, statusCode: (int)HttpStatusCode.BadRequest)
                    .ExecuteAsync(request.HttpContext).ConfigureAwait(false);

                return;
            }

            var result = await engine.ExecuteAsync(body.Query, body.OperationName, body.Variables, cancellationToken).ConfigureAwait(false);

            // Per the GraphQL-over-HTTP convention, the response status remains 200 regardless of field-level errors.
            await Results.Json(new GraphQLLiteResponse(result.Data, result.Errors), JsonDefaults.SerializerOptions, statusCode: (int)HttpStatusCode.OK).ExecuteAsync(request.HttpContext).ConfigureAwait(false);
        })
        .WithName($"CoreExGraphQLLite{pattern.Replace('/', '_')}");

        return endpoints;
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
