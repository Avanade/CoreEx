namespace CoreEx.UnitTesting.Data;

/// <summary>
/// Provides the runtime arguments for the <see cref="JsonDataReader"/>.
/// </summary>
/// <remarks>The dynamic runtime parameters and their corresponding functions.</remarks>
public sealed class JsonDataReaderArgs(IDictionary<string, Func<JsonDataReaderArgs, object?>> parameters)
{
    /// <summary>
    /// Gets the originating <see cref="JsonNode"/> that represents the root for the data.
    /// </summary>
    public JsonNode? Root { get; init; }

    /// <summary>
    /// Gets the current source property name.
    /// </summary>
    /// <remarks>Where the <see cref="CurrentPropertyName"/> is <see langword="null"/> this indicates that no source property is available; i.e. adding new <see cref="Properties"/>.</remarks>
    public string? CurrentPropertyName { get; internal set; }

    /// <summary>
    /// Gets the current source <see cref="JsonNode"/> value.
    /// </summary>
    public JsonNode? CurrentNode { get; internal set; }

    /// <summary>
    /// Gets the current array index where the <see cref="CurrentNode"/> is an element within a <see cref="JsonArray"/>; otherwise, <see langword="null"/>.
    /// </summary>
    public int? Index { get; internal set; }

    /// <summary>
    /// Gets the standard properties that are required for each resulting <see cref="JsonObject"/>.
    /// </summary>
    public IDictionary<string, object?>? Properties { get; internal set; }

    /// <summary>
    /// Gets the dynamic runtime parameters and their corresponding functions.
    /// </summary>
    public IDictionary<string, Func<JsonDataReaderArgs, object?>> Parameters { get; } = parameters;

    /// <summary>
    /// Indicates whether the <see cref="Properties"/> and <see cref="JsonDataReaderOptions.Properties"/> must be applied.
    /// </summary>
    internal bool ApplyProperties { get; set; }
}