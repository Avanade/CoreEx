namespace CoreEx.Azure.Storage.Blobs;

/// <summary>Writes an attempt-isolated, create-only CSV report with explicit columns and terminal outcome metadata.</summary>
/// <remarks>Rows use invariant scalar formatting, UTF-8 without a BOM and CRLF record separators. Null and empty string share an empty field. Await operations sequentially; failures fault the report instance. Completion never appends an out-of-schema summary row.</remarks>
public sealed class BlobCsvReport
{
    private static readonly UTF8Encoding _encoding = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
    private readonly BlobReportWriter _writer;
    private readonly string[] _columns;
    private readonly CsvFormulaHandling _formulaHandling;

    /// <summary>Initializes a CSV attempt with a fixed, ordered column schema and an explicit spreadsheet policy.</summary>
    /// <param name="blob">The attempt's append Blob client. Its container must already exist.</param>
    /// <param name="columns">Nonempty, ordinally distinct column names, copied at construction.</param>
    /// <param name="formulaHandling">The policy for formula-like fields, including headers and formatted negative numbers.</param>
    public BlobCsvReport(AppendBlobClient blob, IReadOnlyList<string> columns, CsvFormulaHandling formulaHandling)
    {
        ArgumentNullException.ThrowIfNull(columns);
        if (!Enum.IsDefined(formulaHandling))
            throw new ArgumentOutOfRangeException(nameof(formulaHandling));

        _columns = [.. columns];
        if (_columns.Length == 0 || _columns.Any(string.IsNullOrWhiteSpace) || _columns.Distinct(StringComparer.Ordinal).Count() != _columns.Length)
            throw new ArgumentException("At least one nonempty, ordinally distinct column name is required.", nameof(columns));

        _formulaHandling = formulaHandling;
        _writer = new BlobReportWriter(blob, "text/csv; charset=utf-8");
    }

    /// <summary>Creates a bounded SHA-256 CSV name from JSON-framed definition/source/revision/attempt components.</summary>
    /// <param name="identityParts">The caller-owned, credential-free identity components, including a distinct attempt.</param>
    /// <returns>A lowercase hash followed by <c>.csv</c>.</returns>
    public static string CreateName(params string?[] identityParts) => BlobReportWriter.CreateName(".csv", identityParts);

    /// <summary>Creates the report with InProgress metadata and awaits exactly one header record before returning.</summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    public Task StartAsync(CancellationToken cancellationToken = default) => _writer.StartAsync(() => SerializeRecord(_columns), cancellationToken);

    /// <summary>Appends one row with exactly the declared number of explicitly ordered cells.</summary>
    /// <param name="values">Null, string or scalar IFormattable/IConvertible values; complex objects require caller projection. Use preformatted strings for schema-specific date or numeric formats.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public Task WriteAsync(IReadOnlyList<object?> values, CancellationToken cancellationToken = default) => _writer.WriteAsync(() => SerializeRecord(values), cancellationToken);

    /// <summary>Sets terminal outcome metadata without writing a summary or changing the CSV schema.</summary>
    /// <param name="outcome">The terminal ASCII outcome other than InProgress.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <remarks>A lost acknowledgement may leave terminal metadata committed despite a propagated failure. Use a distinct attempt, not blind retries.</remarks>
    public Task CompleteAsync(string outcome, CancellationToken cancellationToken = default) => _writer.CompleteAsync(null, outcome, cancellationToken);

    /// <summary>Validates and serializes one complete row before any of its bytes are appended.</summary>
    private byte[] SerializeRecord(IReadOnlyList<object?> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (values.Count != _columns.Length)
            throw new ArgumentException("The row must contain exactly one value per declared column.", nameof(values));

        var text = new StringBuilder();
        for (var index = 0; index < values.Count; index++)
        {
            if (index > 0)
                text.Append(',');

            var value = values[index] switch
            {
                null => "",
                string item => item,
                IFormattable item => item.ToString(null, CultureInfo.InvariantCulture) ?? throw new InvalidOperationException("Scalar formatting returned null."),
                IConvertible item => item.ToString(CultureInfo.InvariantCulture),
                _ => throw new ArgumentException("CSV cells must be null, strings or scalar IFormattable/IConvertible values.", nameof(values))
            };

            if (IsFormulaLike(value))
            {
                if (_formulaHandling == CsvFormulaHandling.Reject)
                    throw new ArgumentException("A CSV field violates the selected spreadsheet formula policy.", nameof(values));

                if (_formulaHandling == CsvFormulaHandling.PrefixApostrophe)
                    value = "'" + value;
            }

            if (value.IndexOfAny([',', '"', '\r', '\n']) >= 0)
                text.Append('"').Append(value.Replace("\"", "\"\"", StringComparison.Ordinal)).Append('"');
            else
                text.Append(value);
        }

        return _encoding.GetBytes(text.Append("\r\n").ToString());
    }

    /// <summary>Detects conservative formula prefixes without relying on CSV quoting as spreadsheet protection.</summary>
    private static bool IsFormulaLike(string value)
    {
        var index = 0;
        while (index < value.Length && char.IsWhiteSpace(value[index]))
        {
            if (value[index] is '\t' or '\r' or '\n')
                return true;

            index++;
        }

        return index < value.Length && value[index] is '=' or '+' or '-' or '@';
    }
}
