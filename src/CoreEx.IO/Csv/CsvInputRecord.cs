namespace CoreEx.IO.Csv;

/// <summary>Holds only the current mapped input record or its local diagnostics.</summary>
/// <typeparam name="TRecord">The wire record type.</typeparam>
/// <param name="Value">The mapped value, null on header/mapping failure.</param>
/// <param name="IsHeader">Whether the input was a column-name header rather than a business record.</param>
/// <param name="Diagnostics">The current header/record diagnostics, never historical diagnostics.</param>
internal sealed record CsvInputRecord<TRecord>(TRecord? Value, bool IsHeader, IReadOnlyList<FileDiagnostic> Diagnostics);
