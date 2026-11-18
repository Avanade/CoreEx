namespace CoreEx.Azure.Storage.Blobs;

/// <summary>Specifies the explicit treatment of spreadsheet-formula-like CSV fields, including column names.</summary>
/// <remarks>Detection includes leading tab/CR/LF, or =, +, - or @ after leading whitespace. This is a conservative output policy, not a guarantee of safe interpretation by every spreadsheet application.</remarks>
public enum CsvFormulaHandling
{
    /// <summary>Preserves supplied values unchanged, accepting the spreadsheet interpretation risk.</summary>
    Preserve = 0,

    /// <summary>Rejects formula-like fields with an exception before appending their row.</summary>
    Reject = 1,

    /// <summary>Prefixes formula-like fields with an apostrophe before CSV quoting, deliberately changing their text.</summary>
    PrefixApostrophe = 2
}
