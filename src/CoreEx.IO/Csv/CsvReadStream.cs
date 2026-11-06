namespace CoreEx.IO.Csv;

/// <summary>Counts reads without transferring ownership of the source to the parser.</summary>
/// <param name="source">The readable stream owned by the processing pass.</param>
/// <param name="onRead">The validation-pass byte counter update.</param>
/// <param name="maxBytes">The maximum source bytes, excluding a single lookahead byte used to prove overflow.</param>
internal sealed class CsvReadStream(Stream source, Action<int> onRead, long maxBytes) : Stream
{
    private readonly byte[] _prefix = new byte[4];
    private int _prefixPosition;
    private int _prefixLength;
    private long _bytesRead;

    /// <summary>Gets whether a one-byte lookahead proved the source exceeds its configured bound.</summary>
    public bool LimitExceeded { get; private set; }

    /// <summary>Reads a bounded prefix, consuming only a matching Unicode BOM without autodetecting another encoding.</summary>
    public async Task<FileDiagnostic?> InitializeAsync(Encoding encoding, CancellationToken cancellationToken)
    {
        while (_prefixLength < _prefix.Length)
        {
            var read = await ReadSourceAsync(_prefix.AsMemory(_prefixLength), cancellationToken).ConfigureAwait(false);
            if (read == 0)
                break;

            _prefixLength += read;
        }

        var (codePage, length) = GetBom();
        if (length == 0)
            return null;

        if (encoding.CodePage != codePage)
            return new(FileDiagnosticCategory.Parsing, "csv.encoding.bom", "The byte order mark conflicts with the configured encoding.");

        _prefixPosition = length;
        return null;
    }

    /// <summary>Identifies supported Unicode signatures, testing UTF-32 before its UTF-16 prefix.</summary>
    private (int CodePage, int Length) GetBom()
    {
        ReadOnlySpan<byte> span = _prefix.AsSpan(0, _prefixLength);
        if (span.StartsWith<byte>([0xff, 0xfe, 0, 0])) return (12000, 4);
        if (span.StartsWith<byte>([0, 0, 0xfe, 0xff])) return (12001, 4);
        if (span.StartsWith<byte>([0xef, 0xbb, 0xbf])) return (65001, 3);
        if (span.StartsWith<byte>([0xff, 0xfe])) return (1200, 2);
        if (span.StartsWith<byte>([0xfe, 0xff])) return (1201, 2);
        return (0, 0);
    }

    /// <inheritdoc/>
    public override bool CanRead => source.CanRead;

    /// <inheritdoc/>
    public override bool CanSeek => false;

    /// <inheritdoc/>
    public override bool CanWrite => false;

    /// <inheritdoc/>
    public override long Length => throw new NotSupportedException();

    /// <inheritdoc/>
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

    /// <inheritdoc/>
    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException("CSV input requires asynchronous reads.");

    /// <inheritdoc/>
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

    /// <inheritdoc/>
    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var prefixCount = Math.Min(buffer.Length, _prefixLength - _prefixPosition);
        if (prefixCount > 0)
        {
            _prefix.AsMemory(_prefixPosition, prefixCount).CopyTo(buffer);
            _prefixPosition += prefixCount;
            return prefixCount;
        }

        return await ReadSourceAsync(buffer, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Never returns over-budget bytes to the decoder and probes at most one extra source byte.</summary>
    private async ValueTask<int> ReadSourceAsync(Memory<byte> buffer, CancellationToken cancellationToken)
    {
        if (buffer.IsEmpty || LimitExceeded)
            return 0;

        if (_bytesRead == maxBytes)
        {
            var probe = new byte[1];
            var extra = await source.ReadAsync(probe, cancellationToken).ConfigureAwait(false);
            onRead(extra);
            LimitExceeded = extra > 0;
            return 0;
        }

        var size = (int)Math.Min(buffer.Length, maxBytes - _bytesRead);
        var read = await source.ReadAsync(buffer[..size], cancellationToken).ConfigureAwait(false);
        _bytesRead += read;
        onRead(read);
        return read;
    }

    /// <inheritdoc/>
    public override void Flush() => throw new NotSupportedException();

    /// <inheritdoc/>
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    /// <inheritdoc/>
    public override void SetLength(long value) => throw new NotSupportedException();

    /// <inheritdoc/>
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
