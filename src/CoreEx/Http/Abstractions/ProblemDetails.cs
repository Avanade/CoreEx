namespace CoreEx.Http.Abstractions;

/// <summary>
/// Represents a problem details for HTTP APIs based on <see href="https://tools.ietf.org/html/rfc7807"/>.
/// </summary>
/// <remarks>This is required for scenarios where there is no explicit ASP.NET Core dependencies to access the out-of-the-box <see href="https://github.com/dotnet/aspnetcore/blob/main/src/Http/Http.Abstractions/src/ProblemDetails/ProblemDetails.cs">ProblemDetails</see>.</remarks>
public class ProblemDetails
{
    /// <summary>
    /// Gets the default key used for validation errors in the <see cref="Extensions"/> dictionary.
    /// </summary>
    /// <remarks>See <see cref="GetValidationErrors(string?)"/>.</remarks>
    public const string ErrorsKey = "errors";

    /// <summary>
    /// Get or sets a URI reference [RFC3986] that identifies the problem type.
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// Gets or sets a short, human-readable summary of the problem type.
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the status code representing the current state of the operation.
    /// </summary>
    [JsonPropertyName("status")]
    public int? Status { get; set; }

    /// <summary>
    /// Gets or sets the detailed description or additional information associated with the object.
    /// </summary>
    [JsonPropertyName("detail")]
    public string? Detail { get; set; }

    /// <summary>
    /// Gets or sets a URI reference that identifies the specific occurrence of the problem.
    /// </summary>
    [JsonPropertyName("instance")]
    public string? Instance { get; set; }

    /// <summary>
    /// Gets a collection of validation errors, grouped by field name.
    /// </summary>
    /// <param name="extensionsKey">The <see cref="Extensions"/> key to use to retrieve the validation errors.</param>
    /// <remarks>Each key in the dictionary represents a field or property name, and the associated value is an array of related error messages for that field.</remarks>
    public IDictionary<string, string[]>? GetValidationErrors(string? extensionsKey = ErrorsKey) 
        => Extensions is not null && Extensions.TryGetValue(extensionsKey ?? ErrorsKey, out var errorsObj) && errorsObj is IDictionary<string, object?> errorsDict
            ? errorsDict.ToDictionary(kvp => kvp.Key, kvp => (kvp.Value as IEnumerable<string>)?.ToArray() ?? [])
            : null;

    /// <summary>
    /// Gets or sets the extensions for the problem details.
    /// </summary>
    [JsonExtensionData]
    public IDictionary<string, object?>? Extensions { get; set; } = new Dictionary<string, object?>(StringComparer.Ordinal);

    /// <summary>
    /// Gets the error type associated with the problem details; see <see cref="IExtendedException.ErrorType"/>.
    /// </summary>
    /// <remarks>This is a <see cref="CoreEx"/>-specific extension to provide additional error context.</remarks>
    [JsonPropertyName("errorType")]
    public string? ErrorType { get; set; }

    /// <summary>
    /// Gets the error code associated with the problem details; see <see cref="IExtendedException.ErrorCode"/>.
    /// </summary>
    /// <remarks>This is a <see cref="CoreEx"/>-specific extension to provide additional error context.</remarks>
    [JsonPropertyName("errorCode")]
    public string? ErrorCode { get; set; }
}