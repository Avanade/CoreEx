namespace CoreEx.IO.Json;

/// <summary>Exposes exactly one selected object's original bytes to the standard asynchronous serializer.</summary>
/// <remarks>Only the current token is borrowed; the underlying document stream remains owned by the processor.</remarks>
internal sealed class JsonSelectedObjectStream : Stream
{
    private readonly JsonDocumentCursor _cursor;
    private readonly int _depth;
    private ReadOnlyMemory<byte> _pending;
    private bool _end;

    /// <summary>Initializes a non-owning view at the selected opening object token.</summary>
    public JsonSelectedObjectStream(JsonDocumentCursor cursor)
    {
        _cursor = cursor;
        _depth = cursor.Token.Depth;
        _pending = cursor.Token.Raw[cursor.Token.TokenStart..];
    }

    /// <summary>Gets a source or reporting JsonException that must propagate rather than become a conversion diagnostic.</summary>
    public JsonException? ReadFailure { get; private set; }

    /// <inheritdoc/>
    public override bool CanRead => true;

    /// <inheritdoc/>
    public override bool CanSeek => false;

    /// <inheritdoc/>
    public override bool CanWrite => false;

    /// <inheritdoc/>
    public override long Length => throw new NotSupportedException();

    /// <inheritdoc/>
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

    /// <inheritdoc/>
    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        var copied = 0;
        while (copied < buffer.Length)
        {
            if (!_pending.IsEmpty)
            {
                var count = Math.Min(buffer.Length - copied, _pending.Length);
                _pending[..count].CopyTo(buffer[copied..]);
                _pending = _pending[count..];
                copied += count;
                continue;
            }

            if (_end)
                break;

            try
            {
                if (!await _cursor.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    _end = true;
                    break;
                }
            }
            catch (JsonException exception)
            {
                ReadFailure = exception;
                throw;
            }

            _pending = _cursor.Token.Raw;
            _end = _cursor.Token.Type == JsonTokenType.EndObject && _cursor.Token.Depth == _depth;
        }

        return copied;
    }

    /// <summary>Finishes structural checking after a serializer conversion failure without retaining remaining bytes.</summary>
    public async Task DrainAsync(CancellationToken cancellationToken)
    {
        _pending = default;
        if (!_end)
        {
            await _cursor.CompleteContainerAsync(_depth, cancellationToken).ConfigureAwait(false);
            _end = true;
        }
    }

    /// <inheritdoc/>
    public override void Flush() => throw new NotSupportedException();

    /// <inheritdoc/>
    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    /// <inheritdoc/>
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    /// <inheritdoc/>
    public override void SetLength(long value) => throw new NotSupportedException();

    /// <inheritdoc/>
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
