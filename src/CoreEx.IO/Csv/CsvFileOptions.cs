namespace CoreEx.IO.Csv;

/// <summary>Defines CSV syntax and decoding independently of parser-owned header and error policies.</summary>
/// <remarks>Defaults to comma-separated fields, doubled double quotes, no comments, and strict UTF-8. A matching Unicode BOM is optional; a conflicting BOM is rejected rather than changing the configured encoding.</remarks>
public sealed record CsvFileOptions
{
    /// <summary>Gets the field delimiter.</summary>
    public char Delimiter { get; init; } = ',';

    /// <summary>Gets the quote character; an embedded quote is represented by doubling it.</summary>
    public char Quote { get; init; } = '"';

    /// <summary>Gets the optional character marking a comment at the beginning of a physical record.</summary>
    public char? Comment { get; init; }

    /// <summary>Gets the explicit input encoding, defaulting to UTF-8. The processor clones it with strict decoding; invalid bytes cause terminal content rejection.</summary>
    public Encoding Encoding { get; init; } = new UTF8Encoding(false, true);

    /// <summary>Gets the maximum physical parser records in one root, including the root and invalid/unknown descendants; defaults to 10,000. Must be positive.</summary>
    public long MaxGroupRecords { get; init; } = 10_000;
}
