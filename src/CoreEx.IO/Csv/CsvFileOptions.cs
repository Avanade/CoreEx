namespace CoreEx.IO.Csv;

/// <summary>Defines CSV syntax and decoding independently of parser-owned header and error policies.</summary>
/// <remarks>Defaults to comma-separated fields, doubled double quotes, no comments, and strict UTF-8. BOM autodetection is disabled; the encoding is explicit. Numeric resource limits and a broader encoding policy remain separate work.</remarks>
public sealed record CsvFileOptions
{
    /// <summary>Gets the field delimiter.</summary>
    public char Delimiter { get; init; } = ',';

    /// <summary>Gets the quote character; an embedded quote is represented by doubling it.</summary>
    public char Quote { get; init; } = '"';

    /// <summary>Gets the optional character marking a comment at the beginning of a physical record.</summary>
    public char? Comment { get; init; }

    /// <summary>Gets the explicit input encoding, defaulting to UTF-8 with invalid-byte exceptions.</summary>
    public Encoding Encoding { get; init; } = new UTF8Encoding(false, true);
}
