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
    /// Gets or sets the <see cref="StringComparer"/> for matching the JSON name (defaults to <see cref="StringComparer.Ordinal"/>).
    /// </summary>
    /// <remarks>RFC 7396 requires case-sensitive member names. A different comparer is a non-standard compatibility option and applies to every object, including dictionary members.</remarks>
    public StringComparer PropertyNameComparer { get; set; } = StringComparer.Ordinal;
}
