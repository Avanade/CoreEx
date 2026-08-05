namespace CoreEx.EntityFrameworkCore.Converters;

/// <summary>
/// Provides a <typeparamref name="T"/> <see cref="ValueComparer{T}"/> that compares by JSON serialization, for use alongside <see cref="TypeToJsonStringEfConverter{T}"/>.
/// </summary>
/// <typeparam name="T">The model type.</typeparam>
public sealed class TypeToJsonStringEfComparer<T>() : ValueComparer<T>(
    (a, b) => JsonSerializer.Serialize(a, JsonDefaults.SerializerOptions) == JsonSerializer.Serialize(b, JsonDefaults.SerializerOptions),
    v => JsonSerializer.Serialize(v, JsonDefaults.SerializerOptions).GetHashCode(),
    v => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(v, JsonDefaults.SerializerOptions), JsonDefaults.SerializerOptions)!)
{
    /// <summary>
    /// Gets the default <see cref="TypeToJsonStringEfComparer{T}"/> instance.
    /// </summary>
    public static TypeToJsonStringEfComparer<T> Default { get; } = new();
}
