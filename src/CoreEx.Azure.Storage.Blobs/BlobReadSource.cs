namespace CoreEx.Azure.Storage.Blobs;

/// <summary>
/// Captures a Blob revision and opens fresh, ETag-conditioned streams for repeatable processing.
/// </summary>
/// <remarks>This is not a snapshot. Replacing or deleting the selected revision causes reads to fail. Supply a version- or snapshot-specific client when historical bytes must remain addressable.</remarks>
public sealed class BlobReadSource
{
    private readonly BlobClient _blob;

    /// <summary>Initializes an already selected source.</summary>
    private BlobReadSource(BlobClient blob, BlobProperties properties)
    {
        _blob = blob;
        ETag = properties.ETag;
        ContentLength = properties.ContentLength;
        SourceUri = GetSourceUri(blob.Uri);
    }

    /// <summary>Gets the credential-free source URI, preserving version/snapshot selectors but no other query values.</summary>
    public Uri SourceUri { get; }

    /// <summary>Gets the selected revision's ETag.</summary>
    public ETag ETag { get; }

    /// <summary>Gets the selected revision's length in bytes for caller-owned admission checks.</summary>
    public long ContentLength { get; }

    /// <summary>Selects the current revision of the supplied client, which may already target a version or snapshot.</summary>
    /// <param name="blob">The caller-owned Blob client, including its authentication and SDK retry configuration.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The selected source; no content is downloaded.</returns>
    public static async Task<BlobReadSource> CreateAsync(BlobClient blob, CancellationToken cancellationToken = default)
    {
        blob.ThrowIfNull();
        var properties = await blob.GetPropertiesAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        return new BlobReadSource(blob, properties.Value);
    }

    /// <summary>Opens a fresh stream conditioned on the selected ETag; the caller must dispose the stream.</summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The read stream. Storage and cancellation failures propagate.</returns>
    public async Task<Stream> OpenReadAsync(CancellationToken cancellationToken = default)
    {
        var response = await _blob.DownloadStreamingAsync(new BlobDownloadOptions
        {
            Conditions = new BlobRequestConditions { IfMatch = ETag }
        }, cancellationToken).ConfigureAwait(false);
        return response.Value.Content;
    }

    /// <summary>Removes authentication/query credentials while retaining explicit immutable revision selectors.</summary>
    private static Uri GetSourceUri(Uri uri)
    {
        var parsed = new BlobUriBuilder(uri);
        var query = new List<string>();
        if (!string.IsNullOrEmpty(parsed.VersionId))
            query.Add("versionid=" + Uri.EscapeDataString(parsed.VersionId));

        if (!string.IsNullOrEmpty(parsed.Snapshot))
            query.Add("snapshot=" + Uri.EscapeDataString(parsed.Snapshot));

        return new UriBuilder(uri) { UserName = "", Password = "", Query = string.Join("&", query), Fragment = "" }.Uri;
    }
}
