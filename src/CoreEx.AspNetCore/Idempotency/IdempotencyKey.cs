namespace CoreEx.AspNetCore.Idempotency;

/// <summary>
/// Provides <see cref="IIdempotencyProvider"/> implementation agnostic data and utility capabilities.
/// </summary>
public sealed partial class IdempotencyKey
{
    /// <summary>
    /// Creates a new instance of the <see cref="IdempotencyKey"/> class.
    /// </summary>
    public static async Task<IdempotencyKey> CreateFromHttpRequestAsync(HttpContext context, IEnumerable<string> headersToIncludeInFingerprint, CancellationToken cancellationToken = default)
    {
        context.ThrowIfNull();
        headersToIncludeInFingerprint.ThrowIfNull();

        using var ih = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        AppendFingerprintField(ih, "CoreEx.AspNetCore.Idempotency.Request:v1");
        AppendFingerprintField(ih, context.Request.Method);
        AppendFingerprintField(ih, context.Request.PathBase.ToString() + context.Request.Path.ToString());
        AppendFingerprintField(ih, context.Request.QueryString.Value ?? string.Empty);

        foreach (var header in headersToIncludeInFingerprint.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(header => header, StringComparer.OrdinalIgnoreCase))
        {
            AppendFingerprintField(ih, header.ToLowerInvariant());
            if (!context.Request.Headers.TryGetValue(header, out var values))
            {
                AppendFingerprintField(ih, "absent");
                continue;
            }

            AppendFingerprintField(ih, "present");
            AppendFingerprintField(ih, values.Count.ToString(CultureInfo.InvariantCulture));
            foreach (var value in values)
                AppendFingerprintField(ih, value?.Trim() ?? string.Empty);
        }

        var originalPosition = context.Request.Body.CanSeek ? context.Request.Body.Position : 0;
        context.Request.EnableBuffering();
        var buffer = new byte[81920];
        try
        {
            int bytesRead;
            while ((bytesRead = await context.Request.Body.ReadAsync(buffer.AsMemory(), cancellationToken).ConfigureAwait(false)) > 0)
                ih.AppendData(buffer, 0, bytesRead);
        }
        finally
        {
            if (context.Request.Body.CanSeek)
                context.Request.Body.Position = originalPosition;
        }

        var hash = ih.GetCurrentHash();
        return new IdempotencyKey { Fingerprint = Convert.ToBase64String(hash) };
    }

    /// <summary>
    /// Appends a length-framed UTF-8 field to the fingerprint hash.
    /// </summary>
    private static void AppendFingerprintField(IncrementalHash hash, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        Span<byte> length = stackalloc byte[sizeof(int)];
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(length, bytes.Length);
        hash.AppendData(length);
        hash.AppendData(bytes);
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

        // Overwrite by key rather than clearing first, so headers already set by earlier middleware for *this* request (CORS, security headers, etc.) survive; the cached
        // headers still win for their own keys, replaying the original response faithfully.
        if (Headers is not null)
            foreach (var header in Headers)
                context.Response.Headers[header.Key] = header.Value;

        await context.Response.Body.WriteAsync(Body).ConfigureAwait(false);
    }
}
