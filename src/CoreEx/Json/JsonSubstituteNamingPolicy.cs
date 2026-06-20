namespace CoreEx.Json;

/// <summary>
/// Provides JSON naming substitution.
/// </summary>
/// <remarks>Defaults the following substitutions: '<c>ETag</c>' and '<c>eTag</c>', to '<c>etag</c>'.
/// <para>Additional <see cref="Substitutions"/> can be added.</para></remarks>
public class JsonSubstituteNamingPolicy : JsonNamingPolicy
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JsonSubstituteNamingPolicy"/> class.
    /// </summary>
    public JsonSubstituteNamingPolicy()
    {
        Substitutions.TryAdd("ETag", "etag");
        Substitutions.TryAdd("eTag", "etag");
    }

    /// <summary>
    /// Gets or sets the fallback policy where no <see cref="Substitutions"/> found.
    /// </summary>
    public JsonNamingPolicy FallbackPolicy { get; set; } = CamelCase;

    /// <summary>
    /// Gets or sets the substitutions.
    /// </summary>
    public ConcurrentDictionary<string, string> Substitutions { get; } = [];

    /// <inheritdoc/>
    /// <remarks>Converts using the <see cref="Substitutions"/> then the <see cref="FallbackPolicy"/>.</remarks>
    public override string ConvertName(string name) => Substitutions.TryGetValue(name, out var substitution) ? substitution : CamelCase.ConvertName(name);
}