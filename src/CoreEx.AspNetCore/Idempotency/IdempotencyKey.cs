namespace CoreEx.AspNetCore.Idempotency;

/// <summary>
/// Provides <see cref="IIdempotencyProvider"/> implementation agnostic data and utility capabilities.
/// </summary>
public sealed partial class IdempotencyKey
{
    /// <summary>
    /// Creates a new instance of the <see cref="IdempotencyKey"/> class.
    /// </summary>
    public static async Task<IdempotencyKey> CreateFromHttpRequestAsync(HttpContext context, IEnumerable<string> headersToIncludeInFingerprint)
    {
        // Fingerprint the request: method, path (no query string) and canonical headers.
        using var ih = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        ih.AppendData(new BinaryData(context.Request.Method.ToUpperInvariant()));
        ih.AppendData(new BinaryData(context.Request.Path.ToString().ToLowerInvariant()));

        foreach (var header in headersToIncludeInFingerprint)
        {
            if (context.Request.Headers.TryGetValue(header, out var values))
            {
                ih.AppendData(new BinaryData(header));

                foreach (var value in values)
                {
                    var val = value?.Trim();
                    if (!string.IsNullOrEmpty(val))
                        ih.AppendData(new BinaryData(val));
                }
            }
        }

        // Read and include the request body, hashing directly in fixed-size chunks rather than copying the whole body into a second, unbounded, always-in-memory buffer
        // (EnableBuffering already provides the safe, disk-spilling copy needed for downstream re-reads).
        context.Request.EnableBuffering();

        var buffer = new byte[81920];
        int bytesRead;
        while ((bytesRead = await context.Request.Body.ReadAsync(buffer.AsMemory(), context.RequestAborted).ConfigureAwait(false)) > 0)
            ih.AppendData(buffer, 0, bytesRead);

        // Rewind stream position for downstream consumption.
        context.Request.Body.Position = 0;

        // Create and return the cached response with the computed request fingerprint.
        var hash = ih.GetCurrentHash();
        return new IdempotencyKey { Fingerprint = Convert.ToBase64String(hash) };
    }

    /// <summary>
    /// Gets or sets the <see cref="Idempotency.IdempotencyStatus"/>.
    /// </summary>
    public IdempotencyStatus Status { get; set; } = IdempotencyStatus.InProgress;

    /// <summary>
    /// Gets or sets the originating request fingerprint.
    /// </summary>
    public string? Fingerprint { get; set; }

    /// <summary>
    /// Gets or sets the response status code.
    /// </summary>
    public int? StatusCode { get; set; }

    /// <summary>
    /// Gets or sets the response headers.
    /// </summary>
    public IDictionary<string, string?[]>? Headers { get; set; }

    /// <summary>
    /// Gets or sets the response body.
    /// </summary>
    public BinaryData? Body { get; set; }

    /// <summary>
    /// Writes the idempotency data to the specified <see cref="HttpContext"/> response.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/>.</param>
    public async Task WriteToHttpResponseAsync(HttpContext context)
    {
        if (context.Response.HasStarted)
            throw new InvalidOperationException("Response already started; cannot replay idempotent response.");

        context.Response.StatusCode = StatusCode ?? 200;

        // Overwrite by key rather than clearing first, so headers already set by earlier middleware for *this* request (CORS, security headers, a fresh correlation id, etc.) survive; the cached
        // headers still win for their own keys, replaying the original response faithfully.
        if (Headers is not null)
        {
            foreach (var header in Headers)
                context.Response.Headers[header.Key] = header.Value;
        }

        await context.Response.Body.WriteAsync(Body).ConfigureAwait(false);
    }
}