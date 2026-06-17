namespace CoreEx.Mapping.Converters;

/// <summary>
/// Represents a <see cref="JsonElement"/> to <see cref="string"/> converter.
/// </summary>
public readonly struct JsonElementStringConverter : IConverter<JsonElement?, string?>
{
    private static readonly ValueConverter<JsonElement?, string?> _convertToDestination = new(s => s?.ToString());
    private static readonly ValueConverter<string?, JsonElement?> _convertToSource = new(d =>
    {
        if (d is null)
            return null;

        using var doc = JsonDocument.Parse(d);
        return doc.RootElement.Clone();
    });

    /// <summary>
    /// Gets or sets the default (singleton) instance.
    /// </summary>
    public static JsonElementStringConverter Default { get; set; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonElementStringConverter"/> struct.
    /// </summary>
    public JsonElementStringConverter() { }

    /// <summary>
    /// Gets the source to destination <see cref="IValueConverter{TSource, TDestination}"/>.
    /// </summary>
    public IValueConverter<JsonElement?, string?> ToDestination => _convertToDestination;

    /// <summary>
    /// Gets the destination to source <see cref="IValueConverter{TDestination, TSource}"/>.
    /// </summary>
    public IValueConverter<string?, JsonElement?> ToSource => _convertToSource;

    /// <inheritdoc />
    public readonly object? ConvertToDestination(object? source) => ConvertToDestination((string?)source);

    /// <inheritdoc />
    public readonly object? ConvertToSource(object? destination) => ConvertToSource((JsonElement?)destination);

    /// <inheritdoc />
    public readonly string? ConvertToDestination(JsonElement? source) => ToDestination.Convert(source);

    /// <inheritdoc />
    public readonly JsonElement? ConvertToSource(string? destination) => ToSource.Convert(destination);
}