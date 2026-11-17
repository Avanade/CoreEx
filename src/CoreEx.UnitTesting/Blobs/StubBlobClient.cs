namespace CoreEx.UnitTesting.Blobs;

/// <summary>Provides an immutable, in-memory Blob fixture for source selection and fresh streaming reads.</summary>
/// <remarks>Only the properties and streaming-download operations used by BlobReadSource are implemented. This stub does not simulate Azure conditional requests, range reads or trigger delivery.</remarks>
/// <param name="content">The fixture content.</param>
/// <param name="uri">The source identity, defaulting to a credential-free test URI.</param>
/// <param name="etag">The selected revision, defaulting to a fixed test revision.</param>
public sealed class StubBlobClient(BinaryData content, Uri? uri = null, global::Azure.ETag etag = default) : BlobClient
{
    private readonly byte[] _content = content.ThrowIfNull().ToArray();

    /// <summary>Creates a fixture from an embedded resource in the calling assembly without changing its bytes.</summary>
    /// <param name="resourceName">The name or unique suffix of the embedded resource.</param>
    /// <param name="uri">The source identity, defaulting to a credential-free test URI.</param>
    /// <param name="etag">The selected revision, defaulting to a fixed test revision.</param>
    /// <returns>An in-memory Blob fixture.</returns>
    /// <remarks>Use the generic overload when the resource is in a different assembly or the assembly must be explicit.</remarks>
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    public static StubBlobClient FromResource(string resourceName, Uri? uri = null, global::Azure.ETag etag = default)
        => FromResource(resourceName, Assembly.GetCallingAssembly(), uri, etag);

    /// <summary>Creates a fixture from an embedded resource without changing its bytes.</summary>
    /// <typeparam name="TResource">A marker type in the assembly containing the resource.</typeparam>
    /// <param name="resourceName">The name or unique suffix of the embedded resource.</param>
    /// <param name="uri">The source identity, defaulting to a credential-free test URI.</param>
    /// <param name="etag">The selected revision, defaulting to a fixed test revision.</param>
    /// <returns>An in-memory Blob fixture.</returns>
    public static StubBlobClient FromResource<TResource>(string resourceName, Uri? uri = null, global::Azure.ETag etag = default)
        => FromResource(resourceName, typeof(TResource).Assembly, uri, etag);

    /// <summary>Loads and snapshots a fixture from the selected assembly.</summary>
    private static StubBlobClient FromResource(string resourceName, Assembly assembly, Uri? uri, global::Azure.ETag etag)
    {
        using var stream = CoreEx.Abstractions.Resource.GetStream(resourceName, assembly);
        return new StubBlobClient(BinaryData.FromStream(stream), uri, etag);
    }

    /// <inheritdoc/>
    public override Uri Uri { get; } = uri ?? new Uri("https://storage.test/imports/input");

    /// <summary>Gets the fixed fixture revision.</summary>
    public global::Azure.ETag ETag { get; } = etag == default ? new global::Azure.ETag("\"fixture\"") : etag;

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
