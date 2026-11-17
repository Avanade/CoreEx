namespace CoreEx.Azure.Storage.Blobs;

/// <summary>
/// Writes an attempt-isolated, create-only append Blob report as compact UTF-8 JSON records.
/// </summary>
/// <param name="blob">The attempt's append Blob client. Its container must already exist.</param>
/// <param name="serializerOptions">The JSON options; defaults to CoreEx options and always disables indentation.</param>
/// <remarks>Await operations sequentially. Overlapping calls are rejected. An I/O, serialization or cancellation failure faults this instance; create a new attempt instead of retrying an ambiguous append. No container provisioning or automatic finalization is performed.</remarks>
public sealed class BlobJsonlReport(AppendBlobClient blob, JsonSerializerOptions? serializerOptions = null)
{
    private readonly BlobReportWriter _writer = new(blob, "application/x-ndjson");
    private readonly JsonSerializerOptions _serializerOptions = new(serializerOptions ?? JsonDefaults.SerializerOptions) { WriteIndented = false };

    /// <summary>Creates a bounded SHA-256 JSONL name from an unambiguously JSON-framed identity sequence.</summary>
    /// <param name="identityParts">Credential-free identity components, including source revision and a distinct attempt identifier.</param>
    /// <returns>A lowercase hash followed by <c>.jsonl</c>.</returns>
    /// <remarks>Identity selection is caller-owned. This helper is not business deduplication or an exactly-once guarantee.</remarks>
    public static string CreateName(params string?[] identityParts)
        => BlobReportWriter.CreateName(".jsonl", identityParts);

    /// <summary>Creates an empty report without replacing any existing Blob and sets its outcome to <c>InProgress</c>.</summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    public Task StartAsync(CancellationToken cancellationToken = default) => _writer.StartAsync(null, cancellationToken);

    /// <summary>Appends one compact JSON record and LF without buffering the report's history.</summary>
    /// <typeparam name="TValue">The record type.</typeparam>
    /// <param name="value">The record to serialize.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public Task WriteAsync<TValue>(TValue value, CancellationToken cancellationToken = default) => _writer.WriteAsync(() => SerializeRecord(value), cancellationToken);

    /// <summary>Appends the terminal record before publishing its explicit terminal outcome in Blob metadata.</summary>
    /// <typeparam name="TValue">The terminal record type.</typeparam>
    /// <param name="value">The caller-defined summary or terminal record.</param>
    /// <param name="outcome">The terminal outcome as an ASCII metadata value other than <c>InProgress</c>.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <remarks>If finalization fails, the Blob may remain InProgress or have accepted the outcome without acknowledgement. No second write or settlement is attempted.</remarks>
    public Task CompleteAsync<TValue>(TValue value, string outcome, CancellationToken cancellationToken = default)
        => _writer.CompleteAsync(() => SerializeRecord(value), outcome, cancellationToken);

    /// <summary>Serializes one compact JSON record followed by LF.</summary>
    private byte[] SerializeRecord<TValue>(TValue value)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value, _serializerOptions);
        Array.Resize(ref bytes, bytes.Length + 1);
        bytes[^1] = (byte)'\n';
        return bytes;
    }
}
