namespace CoreEx.IO.Csv;

/// <summary>Exposes the current parser buffer and a sanitized syntax failure until enumeration advances.</summary>
/// <param name="Reader">The positioned parser.</param>
/// <param name="Diagnostic">The current syntax diagnostic, if any.</param>
internal readonly record struct CsvParsedRecord(CsvReader Reader, FileDiagnostic? Diagnostic);
