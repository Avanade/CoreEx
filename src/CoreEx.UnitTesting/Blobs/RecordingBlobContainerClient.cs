namespace CoreEx.UnitTesting.Blobs;

/// <summary>Records append-Blob reports through SDK-level doubles without an HTTP pipeline or live storage.</summary>
/// <remarks>Supports append-client creation, create-only reports, appends and metadata updates used by BlobJsonlReport/BlobCsvReport. It does not emulate Azure ETag/append-position enforcement, retries or concurrent writers. Use Azurite for storage semantics.</remarks>
public sealed class RecordingBlobContainerClient(Uri? uri = null) : BlobContainerClient
{
    private readonly Dictionary<string, RecordedBlob> _blobs = new(StringComparer.Ordinal);

    /// <inheritdoc/>
    public override Uri Uri { get; } = uri ?? new Uri("https://storage.test/reports");

    /// <summary>Gets successfully created reports keyed by Blob name.</summary>
    public IReadOnlyDictionary<string, RecordedBlob> Blobs => _blobs;

    /// <summary>Gets or sets an action before creating a report, receiving its Blob name.</summary>
    public Action<string>? BeforeCreate { get; set; }

    /// <summary>Gets or sets an action before appending a block, receiving its Blob name.</summary>
    public Action<string>? BeforeAppend { get; set; }

    /// <summary>Gets or sets an action before setting metadata, receiving its Blob name.</summary>
    public Action<string>? BeforeSetMetadata { get; set; }

    /// <inheritdoc/>
    protected override AppendBlobClient GetAppendBlobClientCore(string blobName) => new RecordingAppendBlobClient(this, blobName.ThrowIfNullOrEmpty());

    /// <summary>Records only the append-Blob operations used by the report helpers.</summary>
    private sealed class RecordingAppendBlobClient(RecordingBlobContainerClient owner, string name) : AppendBlobClient
    {
        /// <inheritdoc/>
        public override Uri Uri => new BlobUriBuilder(owner.Uri) { BlobName = name }.ToUri();

        /// <inheritdoc/>
        public override Task<global::Azure.Response<BlobContentInfo>> CreateAsync(AppendBlobCreateOptions options, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            owner.BeforeCreate?.Invoke(name);
            cancellationToken.ThrowIfCancellationRequested();

            var blob = new RecordedBlob(options);
            if (!owner._blobs.TryAdd(name, blob))
                throw new global::Azure.RequestFailedException(409, $"The report '{name}' already exists.");

            return Task.FromResult(global::Azure.Response.FromValue(BlobsModelFactory.BlobContentInfo(blob.ETag, Runtime.UtcNow, null, null, null, null, 0), new BlobTestResponse(201)));
        }

        /// <inheritdoc/>
        public override async Task<global::Azure.Response<BlobAppendInfo>> AppendBlockAsync(Stream content, AppendBlobAppendBlockOptions options, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            owner.BeforeAppend?.Invoke(name);
            cancellationToken.ThrowIfCancellationRequested();

            var blob = GetBlob();
            using var buffer = new MemoryStream();
            await content.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
            blob.Append(buffer.ToArray());
            return global::Azure.Response.FromValue(BlobsModelFactory.BlobAppendInfo(blob.ETag, Runtime.UtcNow, null, null, null, blob.Blocks.Count, false, null, null), new BlobTestResponse(201));
        }

        /// <inheritdoc/>
        public override Task<global::Azure.Response<BlobInfo>> SetMetadataAsync(IDictionary<string, string> metadata, BlobRequestConditions? conditions = null, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            owner.BeforeSetMetadata?.Invoke(name);
            cancellationToken.ThrowIfCancellationRequested();

            var blob = GetBlob();
            blob.SetMetadata(metadata);
            return Task.FromResult(global::Azure.Response.FromValue(BlobsModelFactory.BlobInfo(eTag: blob.ETag, lastModified: Runtime.UtcNow), new BlobTestResponse(200)));
        }

        /// <summary>Requires a successfully created report.</summary>
        private RecordedBlob GetBlob() => owner._blobs.TryGetValue(name, out var blob) ? blob : throw new global::Azure.RequestFailedException(404, $"The report '{name}' has not been created.");
    }
}
