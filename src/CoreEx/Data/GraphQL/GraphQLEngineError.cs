namespace CoreEx.Data.GraphQL;

/// <summary>
/// Represents a single GraphQL-lite execution error; see <see cref="GraphQLEngineResult.Errors"/>.
/// </summary>
/// <remarks>Mirrors the standard GraphQL-over-HTTP error object shape: <c>message</c>, <c>path</c> and <c>extensions</c>.</remarks>
/// <param name="message">The error message.</param>
public sealed class GraphQLEngineError(string message)
{
    /// <summary>
    /// Gets the error message.
    /// </summary>
    public string Message { get; } = message.ThrowIfNull();

    /// <summary>
    /// Gets or sets the path (root field name and, where applicable, nested field segments) to which the error relates.
    /// </summary>
    public IReadOnlyList<string>? Path { get; init; }

    /// <summary>
    /// Gets or sets additional error metadata, for example an error <c>code</c> (e.g. <c>FILTER_PARSE_ERROR</c>, <c>UNKNOWN_FIELD</c>, <c>NOT_FOUND</c>, <c>VALIDATION_ERROR</c>).
    /// </summary>
    public IReadOnlyDictionary<string, object?>? Extensions { get; init; }
}
