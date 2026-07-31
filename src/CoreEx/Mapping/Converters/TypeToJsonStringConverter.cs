namespace CoreEx.Mapping.Converters;

/// <summary>
/// Represents a <typeparamref name="T"/> to JSON <see cref="string"/> converter (uses <see cref="JsonSerializer"/> with <see cref="JsonDefaults.SerializerOptions"/>).
/// </summary>
/// <typeparam name="T"></typeparam>
public readonly struct TypeToJsonStringConverter<T> : IConverter<T, string?>
{
    private static readonly ValueConverter<T, string?> _convertToDestination = new(s => s is null ? null : JsonSerializer.Serialize(s, JsonDefaults.SerializerOptions));
    private static readonly ValueConverter<string?, T> _convertToSource = new(s => s is null ? default! : JsonSerializer.Deserialize<T>(s, JsonDefaults.SerializerOptions)!);

    /// <summary>
    /// Gets or sets the default (singleton) instance.
    /// </summary>
    public static TypeToJsonStringConverter<T> Default { get; set; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="TypeToJsonStringConverter{T}"/> struct.
    /// </summary>
    public TypeToJsonStringConverter() { }

    /// <summary>
    /// Gets the source to destination <see cref="IValueConverter{TSource, TDestination}"/>.
    /// </summary>
    public IValueConverter<T, string?> ToDestination => _convertToDestination;

    /// <summary>
    /// Gets the destination to source <see cref="IValueConverter{TDestination, TSource}"/>.
    /// </summary>
    public IValueConverter<string?, T> ToSource => _convertToSource;

    /// <inheritdoc />
    public readonly object? ConvertToDestination(object? source) => ConvertToDestination((string?)source);

    /// <inheritdoc />
    public readonly object? ConvertToSource(object? destination) => ConvertToSource((byte[]?)destination);

    /// <inheritdoc />
    public readonly string? ConvertToDestination(T source) => ToDestination.Convert(source);

    /// <inheritdoc />
    public readonly T ConvertToSource(string? destination) => ToSource.Convert(destination);
}
