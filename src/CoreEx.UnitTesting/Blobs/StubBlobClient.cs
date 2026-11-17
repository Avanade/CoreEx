namespace CoreEx.UnitTesting.Blobs;

/// <summary>Provides an immutable, in-memory Blob fixture for source selection and fresh streaming reads.</summary>
/// <remarks>Only the properties and streaming-download operations used by BlobReadSource are implemented. This stub does not simulate Azure conditional requests, range reads or trigger delivery.</remarks>
public sealed class StubBlobClient : BlobClient
{
    private readonly byte[] _content;

    /// <summary>Creates a fixture, copying the supplied bytes so subsequent caller mutations cannot change replay input.</summary>
    /// <param name="content">The fixture content.</param>
    /// <param name="uri">The source identity, defaulting to a credential-free test URI.</param>
    /// <param name="etag">The selected revision, defaulting to a fixed test revision.</param>
    public StubBlobClient(BinaryData content, Uri? uri = null, global::Azure.ETag etag = default)
    {
        _content = content.ThrowIfNull().ToArray();
        Uri = uri ?? new Uri("https://storage.test/imports/input");
        ETag = etag == default ? new global::Azure.ETag("\"fixture\"") : etag;
    }

    /// <inheritdoc/>
    public override Uri Uri { get; }

    /// <summary>Gets the fixed fixture revision.</summary>
    public global::Azure.ETag ETag { get; }

    /// <summary>Gets or sets an action before source selection, for counting calls or injecting a failure.</summary>
    public Action? BeforeGetProperties { get; set; }

    /// <summary>Gets or sets an action before each read, for counting calls or injecting a failure.</summary>
    public Action? BeforeRead { get; set; }

    /// <inheritdoc/>
    public override Task<global::Azure.Response<BlobProperties>> GetPropertiesAsync(BlobRequestConditions? conditions = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        BeforeGetProperties?.Invoke();

        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(global::Azure.Response.FromValue(BlobsModelFactory.BlobProperties(contentLength: _content.Length, eTag: ETag), new BlobTestResponse(200)));
    }

    /// <inheritdoc/>
    public override Task<global::Azure.Response<BlobDownloadStreamingResult>> DownloadStreamingAsync(BlobDownloadOptions? options = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        BeforeRead?.Invoke();

        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(global::Azure.Response.FromValue(BlobsModelFactory.BlobDownloadStreamingResult(content: new MemoryStream(_content, writable: false)), new BlobTestResponse(200)));
    }
}
