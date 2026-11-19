namespace CoreEx.AspNetCore.Idempotency;

/// <summary>
/// Provides store-backed HTTP idempotency with owned processing and terminal response records.
/// </summary>
/// <param name="store">The atomic idempotency store.</param>
/// <param name="logger">The optional logger.</param>
public sealed class IdempotencyProvider(CoreEx.Idempotency.IIdempotencyStore store, ILogger<IdempotencyProvider>? logger = null) : IIdempotencyProvider
{
    private const int _defaultMaxCachedResponseBodySize = 512 * 1024;
    private const int _defaultMaxResponseBodySize = 10 * 1024 * 1024;

    private readonly CoreEx.Idempotency.IIdempotencyStore _store = store.ThrowIfNull();
    private readonly ILogger<IdempotencyProvider>? _logger = logger;

    /// <summary>
    /// Gets or sets the maximum response body size retained for replay.
    /// </summary>
    /// <remarks>The default is 512 KB. The store's <see cref="CoreEx.Idempotency.IIdempotencyStore.MaxPayloadSize"/> independently limits the serialized response snapshot. Responses outside either limit are still completed and returned to the original caller, but cannot be replayed.</remarks>
    public int MaxCachedResponseBodySize { get; set => field = value.ThrowWhen(value => value <= 0); } = _defaultMaxCachedResponseBodySize;

    /// <summary>
    /// Gets or sets the maximum response body size captured before the terminal response is committed.
    /// </summary>
    /// <remarks>The default is 10 MB. Capture spills to a temporary file above the smaller of this limit and <see cref="MaxCachedResponseBodySize"/>; exceeding this hard limit fails without exposing a partial response.</remarks>
    public int MaxResponseBodySize { get; set => field = value.ThrowWhen(value => value <= 0); } = _defaultMaxResponseBodySize;

    /// <summary>
    /// Gets or sets the temporary directory used when a captured response spills from memory to disk.
    /// </summary>
    /// <remarks>Defaults to the system temporary directory. Configure this when the host requires a dedicated, bounded temporary-storage location.</remarks>
    public Func<string>? TempFileDirectoryAccessor { get; set; }

    /// <summary>
    /// Gets the list of request headers to include in the request fingerprint.
    /// </summary>
    /// <remarks>Headers are canonicalized by name and value; workflow correlation is excluded by default.</remarks>
    public List<string> HttpRequestHeadersToIncludeInFingerprint { get; } = ["Content-Type"];

    /// <summary>
    /// Gets the list of response headers excluded from replay.
    /// </summary>
    /// <remarks>Cookies and correlation belong to the original caller and are excluded by default.</remarks>
    public List<string> HttpResponseHeadersToExcludeFromCache { get; } = [HeaderNames.SetCookie, HttpNames.CorrelationIdHeaderName];

    /// <summary>
    /// Gets or sets the optional stable caller identity provider used to scope HTTP idempotency keys.
    /// </summary>
    /// <remarks>By default, an authenticated HTTP principal's name-identifier or subject claim is used when present. The returned identity is hashed before it is included in the store key. Domain and tenant qualification remains the store's responsibility through <see cref="ICacheKeyProvider"/>.</remarks>
    public Func<HttpContext, string?>? CallerIdentityProvider { get; set; }

    /// <summary>
    /// Gets or sets whether incoming keys must use the standard quoted Structured Field string representation.
    /// </summary>
    /// <remarks>The default is <see langword="false"/>, which also accepts the documented legacy bare-key format.</remarks>
    public bool StrictHeaderValidation { get; set; }

    /// <summary>
    /// Gets or sets the absolute HTTP documentation URI advertised through a describedby Link header.
    /// </summary>
    public Uri? DocumentationUri
    {
        get;
        set
        {
            if (value is not null && (!value.IsAbsoluteUri || value.Scheme is not ("http" or "https")))
                throw new ArgumentException("Idempotency documentation must have an absolute HTTP or HTTPS URI.", nameof(value));

            field = value;
        }
    }

    /// <inheritdoc/>
    public async Task OnInvokeAsync(IdempotencyKeyAttribute attribute, HttpContext context, RequestDelegate next, CancellationToken cancellationToken = default)
    {
        attribute.ThrowIfNull();
        context.ThrowIfNull();
        next.ThrowIfNull();
        if (DocumentationUri is not null)
            context.Response.Headers.Append(HeaderNames.Link, $"<{DocumentationUri.AbsoluteUri}>; rel=\"describedby\"");

        using var requestCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, context.RequestAborted);
        var requestToken = requestCancellation.Token;
        var key = GetIdempotencyKey(attribute, context);
        if (key is null)
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        var fingerprint = await IdempotencyKey.CreateFromHttpRequestAsync(context, HttpRequestHeadersToIncludeInFingerprint, requestToken).ConfigureAwait(false);
        var callerIdentity = CallerIdentityProvider is null ? GetAuthenticatedCaller(context) : CallerIdentityProvider(context);
        var storeKey = CreateStoreKey(key, callerIdentity);
        CoreEx.Idempotency.IdempotencyBeginResult begin;

        try
        {
            begin = await _store.TryBeginAsync(storeKey, fingerprint.Fingerprint ?? throw new InvalidOperationException("The request fingerprint was not created."), requestToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (requestToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw IdempotencyKey.CreateStoreUnavailableException(ex);
        }

        switch (begin.Outcome)
        {
            case CoreEx.Idempotency.IdempotencyOutcome.InProgress:
                Activity.Current?.SetTag("idempotency.result", "is-in-progress");
                throw IdempotencyKey.CreateInProgressException();

            case CoreEx.Idempotency.IdempotencyOutcome.Indeterminate:
                Activity.Current?.SetTag("idempotency.result", "outcome-indeterminate");
                throw IdempotencyKey.CreateIndeterminateException();

            case CoreEx.Idempotency.IdempotencyOutcome.DifferentRequest:
                Activity.Current?.SetTag("idempotency.result", "different-request");
                throw IdempotencyKey.CreateUsedForDifferentRequestException();

            case CoreEx.Idempotency.IdempotencyOutcome.ResponseUnavailable:
                Activity.Current?.SetTag("idempotency.result", "response-unavailable");
                throw IdempotencyKey.CreateResponseTooLargeException();

            case CoreEx.Idempotency.IdempotencyOutcome.Completed:
                Activity.Current?.SetTag("idempotency.result", "replayed");
                await WriteSnapshotAsync(context, begin.Response ?? throw new InvalidDataException("A completed idempotency result did not contain a response snapshot."), requestToken).ConfigureAwait(false);
                return;

            case CoreEx.Idempotency.IdempotencyOutcome.Acquired:
                if (begin.Attempt is null)
                    throw new InvalidDataException("The idempotency store returned an acquired outcome without an owned attempt.");
                await ExecuteOwnedAsync(context, next, begin.Attempt, requestToken).ConfigureAwait(false);
                return;

            default:
                throw new InvalidOperationException($"The idempotency store returned an unsupported outcome '{begin.Outcome}'.");
        }
    }

    /// <summary>
    /// Validates and returns the logical key, or null when the request does not opt into idempotency.
    /// </summary>
    private string? GetIdempotencyKey(IdempotencyKeyAttribute attribute, HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue(HttpNames.IdempotencyKeyHeaderName, out var values))
        {
            if (attribute.IsRequired)
                throw IdempotencyKey.CreateIdempotencyKeyRequiredException();

            return null;
        }

        if (values.Count != 1 || !IdempotencyKey.TryParseHeader(values[0], StrictHeaderValidation, out var key))
            throw IdempotencyKey.CreateInvalidHeaderException();

        return key;
    }

    /// <summary>
    /// Executes and captures one owned operation, committing its terminal response before exposing the body.
    /// </summary>
    private async Task ExecuteOwnedAsync(HttpContext context, RequestDelegate next, CoreEx.Idempotency.IIdempotencyAttempt attempt, CancellationToken requestToken)
    {
        Exception? workException = null;
        try
        {
            await ExecuteCapturedAsync(context, next, attempt, requestToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            workException = ex;
            throw;
        }
        finally
        {
            try
            {
                await attempt.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception cleanupException) when (workException is not null)
            {
                _logger?.LogError(cleanupException, "Disposing an idempotency attempt failed while preserving the original operation failure.");
            }
        }
    }

    /// <summary>
    /// Captures a bounded response and commits the terminal outcome before draining it to the caller.
    /// </summary>
    private async Task ExecuteCapturedAsync(HttpContext context, RequestDelegate next, CoreEx.Idempotency.IIdempotencyAttempt attempt, CancellationToken requestToken)
    {
        using var operationCancellation = CancellationTokenSource.CreateLinkedTokenSource(requestToken, attempt.OwnershipLost);
        var operationToken = operationCancellation.Token;
        var originalBody = context.Response.Body;
        var originalBodyFeature = context.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpResponseBodyFeature>() ?? throw new InvalidOperationException("The HTTP response body feature is unavailable.");
        var originalRequestAborted = context.RequestAborted;

        await using var buffer = new Microsoft.AspNetCore.WebUtilities.FileBufferingWriteStream(Math.Min(MaxCachedResponseBodySize, MaxResponseBodySize), MaxResponseBodySize, TempFileDirectoryAccessor ?? System.IO.Path.GetTempPath);
        await using var captureFeature = new CapturingResponseBodyFeature(buffer);

        context.Features.Set<Microsoft.AspNetCore.Http.Features.IHttpResponseBodyFeature>(captureFeature);
        context.RequestAborted = operationToken;

        try
        {
            Activity.Current?.SetTag("idempotency.result", "processing");
            operationToken.ThrowIfCancellationRequested();
            await next(context).ConfigureAwait(false);
            operationToken.ThrowIfCancellationRequested();
            if (!ReferenceEquals(context.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpResponseBodyFeature>(), captureFeature) || captureFeature.BufferingDisabled || captureFeature.CaptureFailed)
                throw new InvalidOperationException("The response changed to an unsupported streaming mode during idempotency capture.");

            if (context.Response.HasStarted)
                throw new InvalidOperationException("The response started before its idempotent terminal outcome was committed.");

            await captureFeature.FlushAsync(operationToken).ConfigureAwait(false);
            await buffer.FlushAsync(operationToken).ConfigureAwait(false);

            var snapshot = buffer.Length <= MaxCachedResponseBodySize ? await CreateResponseSnapshotAsync(context, buffer, operationToken).ConfigureAwait(false) : null;
            var response = snapshot is null ? null : SerializeResponseSnapshot(snapshot);
            if (response is not null && _store.MaxPayloadSize is int maxPayloadSize && response.ToMemory().Length > maxPayloadSize)
                response = null;

            try
            {
                await attempt.CompleteAsync(response, operationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (operationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw IdempotencyKey.CreateStoreUnavailableException(ex);
            }

            if (response is null)
            {
                Activity.Current?.SetTag("idempotency.result", "completed-response-unavailable");
                _logger?.LogWarning("The completed idempotent response of {ResponseBodySize} bytes could not be retained for replay (body limit {MaxCachedResponseBodySize} bytes; store payload limit {MaxPayloadSize} bytes, if configured).", buffer.Length, MaxCachedResponseBodySize, _store.MaxPayloadSize);
            }
            else
                Activity.Current?.SetTag("idempotency.result", "completed-and-stored");

            context.Features.Set(originalBodyFeature);
            if (snapshot is not null)
                await WriteCapturedResponseAsync(context, snapshot, operationToken).ConfigureAwait(false);
            else
            {
                context.Response.ContentLength = buffer.Length;
                await buffer.DrainBufferAsync(originalBody, operationToken).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            captureFeature.Abort(ex);
            throw;
        }
        finally
        {
            context.Features.Set(originalBodyFeature);
            context.RequestAborted = originalRequestAborted;
        }
    }

    /// <summary>
    /// Serializes the bounded response body and replayable metadata.
    /// </summary>
    private async Task<ResponseSnapshot> CreateResponseSnapshotAsync(HttpContext context, Microsoft.AspNetCore.WebUtilities.FileBufferingWriteStream body, CancellationToken cancellationToken)
    {
        await using var copy = new MemoryStream();
        await body.DrainBufferAsync(copy, cancellationToken).ConfigureAwait(false);

        var headers = context.Response.Headers
            .Where(header => !HttpResponseHeadersToExcludeFromCache.Contains(header.Key, StringComparer.OrdinalIgnoreCase))
            .ToDictionary(header => header.Key, header => header.Value.Select(value => value ?? string.Empty).ToArray(), StringComparer.OrdinalIgnoreCase);

        return new ResponseSnapshot(1, context.Response.StatusCode, headers, copy.ToArray());
    }

    /// <summary>
    /// Serializes a response snapshot for the store.
    /// </summary>
    private static BinaryData SerializeResponseSnapshot(ResponseSnapshot snapshot) => BinaryData.FromBytes(JsonSerializer.SerializeToUtf8Bytes(snapshot, JsonDefaults.SerializerOptions));

    /// <summary>
    /// Writes a captured response without deserializing its serialized store representation.
    /// </summary>
    private static async Task WriteCapturedResponseAsync(HttpContext context, ResponseSnapshot snapshot, CancellationToken cancellationToken)
    {
        if (context.Response.HasStarted)
            throw new InvalidOperationException("Response already started; cannot write a captured idempotent response.");

        context.Response.StatusCode = snapshot.StatusCode;
        foreach (var header in snapshot.Headers)
            context.Response.Headers[header.Key] = header.Value;

        context.Response.ContentLength = snapshot.Body.Length;
        await context.Response.Body.WriteAsync(snapshot.Body, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Deserializes and writes a previously committed response.
    /// </summary>
    private static async Task WriteSnapshotAsync(HttpContext context, BinaryData payload, CancellationToken cancellationToken)
    {
        if (context.Response.HasStarted)
            throw new InvalidOperationException("Response already started; cannot replay an idempotent response.");

        var snapshot = JsonSerializer.Deserialize<ResponseSnapshot>(payload.ToMemory().Span, JsonDefaults.SerializerOptions);
        if (snapshot is null || snapshot.Version != 1)
            throw new InvalidDataException("The stored idempotency response snapshot has an unsupported format.");

        await WriteCapturedResponseAsync(context, snapshot, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Constructs the versioned HTTP store key without exposing the caller identity or logical key.
    /// </summary>
    private static string CreateStoreKey(string key, string? callerIdentity)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        AppendField(hash, "CoreEx.AspNetCore.Idempotency:v1");
        AppendField(hash, callerIdentity ?? string.Empty);
        AppendField(hash, key);
        return $"http-idempotency:v1:{Convert.ToHexString(hash.GetHashAndReset())}";
    }

    /// <summary>
    /// Appends a length-framed UTF-8 field to a hash.
    /// </summary>
    private static void AppendField(IncrementalHash hash, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        Span<byte> length = stackalloc byte[sizeof(int)];
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(length, bytes.Length);
        hash.AppendData(length);
        hash.AppendData(bytes);
    }

    /// <summary>
    /// Gets a stable authenticated caller identifier when the current HTTP principal provides one.
    /// </summary>
    private static string? GetAuthenticatedCaller(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated != true)
            return null;

        var claim = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier) ?? context.User.FindFirst("sub");
        return claim is null ? null : JsonSerializer.Serialize(new[] { claim.Type, claim.Issuer, claim.Value });
    }

    /// <summary>
    /// Captures the response body without starting or completing the host response.
    /// </summary>
    private sealed class CapturingResponseBodyFeature : Microsoft.AspNetCore.Http.Features.IHttpResponseBodyFeature, IAsyncDisposable
    {
        private readonly CaptureResponseStream _stream;
        private readonly System.IO.Pipelines.PipeWriter _writer;
        private Exception? _failure;

        /// <summary>
        /// Initializes a response-body feature backed by a bounded capture stream.
        /// </summary>
        /// <param name="stream">The bounded underlying buffer.</param>
        public CapturingResponseBodyFeature(Stream stream)
        {
            _stream = new CaptureResponseStream(stream.ThrowIfNull());
            _writer = System.IO.Pipelines.PipeWriter.Create(_stream, new System.IO.Pipelines.StreamPipeWriterOptions(leaveOpen: true));
        }

        /// <summary>
        /// Gets whether the application requested that buffering be disabled.
        /// </summary>
        public bool BufferingDisabled { get; private set; }

        /// <summary>
        /// Gets whether a response-body write failed, even if the application caught the failure.
        /// </summary>
        public bool CaptureFailed => _stream.CaptureFailed;

        /// <summary>
        /// Prevents pending writer bytes from being flushed during failed-response cleanup.
        /// </summary>
        public void Abort(Exception failure) => _failure ??= failure;

        /// <inheritdoc/>
        public Stream Stream => _stream;

        /// <inheritdoc/>
        public System.IO.Pipelines.PipeWriter Writer => _writer;

        /// <inheritdoc/>
        public void DisableBuffering() => BufferingDisabled = true;

        /// <inheritdoc/>
        public Task StartAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        /// <inheritdoc/>
        public async Task SendFileAsync(string path, long offset, long? count, CancellationToken cancellationToken = default)
        {
            if (offset < 0 || count is < 0)
                throw new ArgumentOutOfRangeException(nameof(offset));

            await using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
            file.Position = offset;
            if (count is null)
            {
                await file.CopyToAsync(_stream, cancellationToken).ConfigureAwait(false);
                return;
            }

            var remaining = count.Value;
            var buffer = new byte[81920];
            while (remaining > 0)
            {
                var read = await file.ReadAsync(buffer.AsMemory(0, (int)Math.Min(buffer.Length, remaining)), cancellationToken).ConfigureAwait(false);
                if (read == 0)
                    break;

                await _stream.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                remaining -= read;
            }
        }

        /// <inheritdoc/>
        public async Task CompleteAsync()
        {
            await FlushAsync(CancellationToken.None).ConfigureAwait(false);
        }

        /// <summary>
        /// Flushes buffered body-writer data into the bounded capture stream.
        /// </summary>
        public async Task FlushAsync(CancellationToken cancellationToken)
        {
            var result = await _writer.FlushAsync(cancellationToken).ConfigureAwait(false);
            if (result.IsCanceled)
                cancellationToken.ThrowIfCancellationRequested();
        }

        /// <inheritdoc/>
        public ValueTask DisposeAsync() => _writer.CompleteAsync(_failure);
    }

    /// <summary>
    /// Tracks capture failures on the response stream so swallowed buffering errors cannot commit a truncated response.
    /// </summary>
    private sealed class CaptureResponseStream(Stream stream) : Stream
    {
        private readonly Stream _stream = stream.ThrowIfNull();

        /// <summary>
        /// Gets whether a write or flush failed.
        /// </summary>
        public bool CaptureFailed { get; private set; }

        /// <inheritdoc/>
        public override bool CanRead => _stream.CanRead;

        /// <inheritdoc/>
        public override bool CanSeek => _stream.CanSeek;

        /// <inheritdoc/>
        public override bool CanWrite => _stream.CanWrite;

        /// <inheritdoc/>
        public override long Length => _stream.Length;

        /// <inheritdoc/>
        public override long Position { get => _stream.Position; set => _stream.Position = value; }

        /// <inheritdoc/>
        public override void Flush() => RunCapture(_stream.Flush);

        /// <inheritdoc/>
        public override Task FlushAsync(CancellationToken cancellationToken) => RunCaptureAsync(() => _stream.FlushAsync(cancellationToken));

        /// <inheritdoc/>
        public override int Read(byte[] buffer, int offset, int count) => _stream.Read(buffer, offset, count);

        /// <inheritdoc/>
        public override int Read(Span<byte> buffer) => _stream.Read(buffer);

        /// <inheritdoc/>
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => _stream.ReadAsync(buffer, offset, count, cancellationToken);

        /// <inheritdoc/>
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) => _stream.ReadAsync(buffer, cancellationToken);

        /// <inheritdoc/>
        public override long Seek(long offset, SeekOrigin origin) => _stream.Seek(offset, origin);

        /// <inheritdoc/>
        public override void SetLength(long value) => _stream.SetLength(value);

        /// <inheritdoc/>
        public override void Write(byte[] buffer, int offset, int count) => RunCapture(() => _stream.Write(buffer, offset, count));

        /// <inheritdoc/>
        public override void Write(ReadOnlySpan<byte> buffer)
        {
            try
            {
                _stream.Write(buffer);
            }
            catch
            {
                CaptureFailed = true;
                throw;
            }
        }

        /// <inheritdoc/>
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => RunCaptureAsync(() => _stream.WriteAsync(buffer, offset, count, cancellationToken));

        /// <inheritdoc/>
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) => RunCaptureAsync(() => _stream.WriteAsync(buffer, cancellationToken));

        /// <summary>
        /// Executes a synchronous capture operation and records any failure.
        /// </summary>
        private void RunCapture(Action action)
        {
            try
            {
                action();
            }
            catch
            {
                CaptureFailed = true;
                throw;
            }
        }

        /// <summary>
        /// Executes an asynchronous capture operation and records any failure.
        /// </summary>
        private async Task RunCaptureAsync(Func<Task> action)
        {
            try
            {
                await action().ConfigureAwait(false);
            }
            catch
            {
                CaptureFailed = true;
                throw;
            }
        }

        /// <summary>
        /// Executes an asynchronous capture operation and records any failure.
        /// </summary>
        private async ValueTask RunCaptureAsync(Func<ValueTask> action)
        {
            try
            {
                await action().ConfigureAwait(false);
            }
            catch
            {
                CaptureFailed = true;
                throw;
            }
        }
    }

    /// <summary>
    /// Defines the versioned, replayable HTTP response representation.
    /// </summary>
    /// <param name="Version">The format version.</param>
    /// <param name="StatusCode">The HTTP status code.</param>
    /// <param name="Headers">The replayable response headers.</param>
    /// <param name="Body">The response body.</param>
    private sealed record ResponseSnapshot(int Version, int StatusCode, Dictionary<string, string[]> Headers, byte[] Body);
}
