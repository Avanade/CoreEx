namespace CoreEx.Data.GraphQL;

/// <summary>
/// Represents the result of an <see cref="IGraphQLEngine.ExecuteAsync(string, string?, IReadOnlyDictionary{string, object?}?, CancellationToken)"/> execution.
/// </summary>
/// <remarks>Mirrors the standard GraphQL-over-HTTP response envelope shape (<c>data</c>/<c>errors</c>) so it can be serialized directly by any transport.</remarks>
public sealed class GraphQLEngineResult
{
    /// <summary>
    /// Gets or sets the resulting data payload (the resolved root field(s) and any sibling paging metadata).
    /// </summary>
    public JsonElement? Data { get; set; }

    /// <summary>
    /// Gets or sets the list of errors encountered during execution (see <see cref="GraphQLEngineError"/>).
    /// </summary>
    public IReadOnlyList<GraphQLEngineError>? Errors { get; set; }

    /// <summary>
    /// Indicates whether the result contains one or more <see cref="Errors"/>.
    /// </summary>
    public bool HasErrors => Errors is not null && Errors.Count > 0;

    /// <summary>
    /// Creates a new <see cref="GraphQLEngineResult"/> with the specified <paramref name="data"/>.
    /// </summary>
    /// <param name="data">The resulting data payload.</param>
    /// <returns>The <see cref="GraphQLEngineResult"/>.</returns>
    public static GraphQLEngineResult Success(JsonElement data) => new() { Data = data };

    /// <summary>
    /// Creates a new <see cref="GraphQLEngineResult"/> with the specified <paramref name="errors"/>.
    /// </summary>
    /// <param name="errors">The list of errors.</param>
    /// <returns>The <see cref="GraphQLEngineResult"/>.</returns>
    public static GraphQLEngineResult Failure(params IEnumerable<GraphQLEngineError> errors) => new() { Errors = [.. errors] };
}
