namespace CoreEx.Azure.Storage.Blobs;

/// <summary>Shares the create-only, conditional append and explicit-completion lifecycle between report formats.</summary>
/// <param name="blob">The attempt's append Blob client.</param>
/// <param name="contentType">The format's content type.</param>
internal sealed class BlobReportWriter(AppendBlobClient blob, string contentType)
{
    private readonly AppendBlobClient _blob = blob.ThrowIfNull();
    private bool _started;
    private bool _finished;
    private bool _faulted;
    private int _busy;
    private long _position;
    private ETag _etag;

    /// <summary>Creates a bounded name from an unambiguously JSON-framed identity sequence.</summary>
    public static string CreateName(string extension, string?[] identityParts)
    {
        ArgumentNullException.ThrowIfNull(identityParts);
        if (identityParts.Length == 0)
            throw new ArgumentException("At least one identity component is required.", nameof(identityParts));

        return Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(identityParts))).ToLowerInvariant() + extension;
    }

    /// <summary>Creates a new attempt and optionally appends its header while holding exclusive ownership.</summary>
    public async Task StartAsync(Func<byte[]>? header, CancellationToken cancellationToken)
    {
        Enter();
        try
        {
            if (_started)
                throw new InvalidOperationException("The report has already started.");

            cancellationToken.ThrowIfCancellationRequested();
            var bytes = header?.Invoke();

            var response = await _blob.CreateAsync(new AppendBlobCreateOptions
            {
                Conditions = new AppendBlobRequestConditions { IfNoneMatch = ETag.All },
                HttpHeaders = new BlobHttpHeaders { ContentType = contentType },
                Metadata = new Dictionary<string, string> { ["outcome"] = "InProgress" }
            }, cancellationToken).ConfigureAwait(false);

            _etag = response.Value.ETag;
            _started = true;
            if (bytes is not null)
                await AppendAsync(bytes, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            _faulted = true;
            throw;
        }
        finally
        {
            Volatile.Write(ref _busy, 0);
        }
    }

    /// <summary>Formats and appends one record without permitting concurrent or post-failure writes.</summary>
    public async Task WriteAsync(Func<byte[]> record, CancellationToken cancellationToken)
    {
        Enter();
        try
        {
            CheckWritable();
            cancellationToken.ThrowIfCancellationRequested();
            await AppendAsync(record(), cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            _faulted = true;
            throw;
        }
        finally
        {
            Volatile.Write(ref _busy, 0);
        }
    }

    /// <summary>Optionally appends a terminal record before setting terminal outcome metadata.</summary>
    public async Task CompleteAsync(Func<byte[]>? record, string outcome, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(outcome) || outcome.Equals("InProgress", StringComparison.OrdinalIgnoreCase) || outcome.Any(c => c < 32 || c > 126))
            throw new ArgumentException("A terminal ASCII outcome other than InProgress is required.", nameof(outcome));

        Enter();
        try
        {
            CheckWritable();
            cancellationToken.ThrowIfCancellationRequested();
            if (record is not null)
                await AppendAsync(record(), cancellationToken).ConfigureAwait(false);

            await _blob.SetMetadataAsync(new Dictionary<string, string> { ["outcome"] = outcome }, new BlobRequestConditions { IfMatch = _etag }, cancellationToken).ConfigureAwait(false);
            _finished = true;
        }
        catch
        {
            _faulted = true;
            throw;
        }
        finally
        {
            Volatile.Write(ref _busy, 0);
        }
    }

    /// <summary>Rejects concurrent use and any reuse after an ambiguous failure.</summary>
    private void Enter()
    {
        if (Interlocked.CompareExchange(ref _busy, 1, 0) != 0)
            throw new InvalidOperationException("Report operations must be awaited sequentially.");

        if (_faulted)
        {
            Volatile.Write(ref _busy, 0);
            throw new InvalidOperationException("The report has faulted; use a new attempt rather than retrying an ambiguous write.");
        }
    }

    /// <summary>Requires an active, unfinalized report.</summary>
    private void CheckWritable()
    {
        if (!_started || _finished)
            throw new InvalidOperationException("The report must be started and not completed.");
    }

    /// <summary>Appends at the expected position and ETag to detect conflicting or ambiguously acknowledged writes.</summary>
    private async Task AppendAsync(byte[] bytes, CancellationToken cancellationToken)
    {
        using var content = new MemoryStream(bytes, writable: false);
        var response = await _blob.AppendBlockAsync(content, new AppendBlobAppendBlockOptions
        {
            Conditions = new AppendBlobRequestConditions { IfMatch = _etag, IfAppendPositionEqual = _position }
        }, cancellationToken).ConfigureAwait(false);

        _position += bytes.Length;
        _etag = response.Value.ETag;
    }
}
