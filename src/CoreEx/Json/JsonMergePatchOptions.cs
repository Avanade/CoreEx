namespace CoreEx.Json;

/// <summary>
/// The <see cref="JsonMergePatch"/> options.
/// </summary>
/// <param name="jsonSerializerOptions">The optional <see cref="JsonSerializerOptions"/>.</param>
public class JsonMergePatchOptions(JsonSerializerOptions? jsonSerializerOptions = null)
{
    /// <summary>
    /// Gets the <see cref="JsonSerializerOptions"/>.
    /// </summary>
    public JsonSerializerOptions JsonSerializerOptions { get; } = jsonSerializerOptions ?? JsonDefaults.SerializerOptions;

    /// <summary>
    /// Gets or sets the <see cref="StringComparer"/> for matching the JSON name (defaults to <see cref="StringComparer.OrdinalIgnoreCase"/>).
    /// </summary>
    public StringComparer PropertyNameComparer { get; set; } = StringComparer.OrdinalIgnoreCase;
}