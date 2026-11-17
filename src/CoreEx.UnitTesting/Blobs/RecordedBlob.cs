namespace CoreEx.UnitTesting.Blobs;

/// <summary>Captures the opaque append blocks and metadata written to one test report Blob.</summary>
/// <remarks>Contents are buffered for bounded test fixtures only. JSONL/CSV parsing and business assertions remain the test's responsibility.</remarks>
public sealed class RecordedBlob
{
    private readonly List<BinaryData> _blocks = [];
    private Dictionary<string, string> _metadata;
    private int _revision = 1;

    /// <summary>Creates the initial captured report.</summary>
    internal RecordedBlob(AppendBlobCreateOptions options)
    {
        ContentType = options.HttpHeaders?.ContentType;
        _metadata = options.Metadata is null ? [] : new(options.Metadata);
    }

    /// <summary>Gets the declared report content type.</summary>
    public string? ContentType { get; }

    /// <summary>Gets the appended blocks in write order.</summary>
    public IReadOnlyList<BinaryData> Blocks => _blocks.AsReadOnly();

    /// <summary>Gets a combined copy of the report content.</summary>
    public BinaryData Content => new(_blocks.SelectMany(b => b.ToArray()).ToArray());

    /// <summary>Gets the latest metadata, including a terminal outcome when the application wrote one.</summary>
    public IReadOnlyDictionary<string, string> Metadata => _metadata;

    /// <summary>Gets a synthetic ETag for SDK response models, not Azure concurrency enforcement.</summary>
    internal global::Azure.ETag ETag => new($"\"test-{_revision}\"");

    /// <summary>Appends a copied block and advances the response revision.</summary>
    internal void Append(byte[] content)
    {
        _blocks.Add(new BinaryData(content));
        _revision++;
    }

    /// <summary>Replaces captured metadata and advances the response revision.</summary>
    internal void SetMetadata(IDictionary<string, string> metadata)
    {
        _metadata = new(metadata);
        _revision++;
    }
}
