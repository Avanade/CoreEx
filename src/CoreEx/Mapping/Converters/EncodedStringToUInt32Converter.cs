namespace CoreEx.Mapping.Converters;

/// <summary>
/// Represents a BASE64 encoded <see cref="string"/> to <see cref="uint"/> converter.
/// </summary>
public readonly struct EncodedStringToUInt32Converter : IConverter<string?, uint>
{
    private static readonly ValueConverter<string?, uint> _convertToDestination = new(s =>
    {
        if (s == null)
            return 0;

        var bytes = Convert.FromBase64String(s);
        return bytes.Length == 4
            ? BitConverter.ToUInt32(bytes)
            : throw new FormatException($"The decoded value must be exactly 4 bytes to convert to a {nameof(UInt32)}; the specified value decoded to {bytes.Length} byte(s).");
    });
    private static readonly ValueConverter<uint, string?> _convertToSource = new(d => d == 0 ? null : Convert.ToBase64String(BitConverter.GetBytes(d)));

    /// <summary>
    /// Gets or sets the default (singleton) instance.
    /// </summary>
    public static EncodedStringToUInt32Converter Default { get; set; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="EncodedStringToUInt32Converter"/> struct.
    /// </summary>
    public EncodedStringToUInt32Converter() { }

    /// <summary>
    /// Gets the source to destination <see cref="IValueConverter{TSource, TDestination}"/>.
    /// </summary>
    public IValueConverter<string?, uint> ToDestination => _convertToDestination;

    /// <summary>
    /// Gets the destination to source <see cref="IValueConverter{TDestination, TSource}"/>.
    /// </summary>
    public IValueConverter<uint, string?> ToSource => _convertToSource;

    /// <inheritdoc />
    public readonly uint ConvertToDestination(string? source) => ToDestination.Convert(source);

    /// <inheritdoc />
    public readonly string? ConvertToSource(uint destination) => ToSource.Convert(destination);
}