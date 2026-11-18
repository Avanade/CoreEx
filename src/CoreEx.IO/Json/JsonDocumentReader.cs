namespace CoreEx.IO.Json;

/// <summary>Reads incremental JSON tokens while preserving token bytes and physical provenance.</summary>
/// <param name="source">The non-seekable input stream.</param>
/// <param name="maxDepth">The configured maximum nesting depth.</param>
/// <param name="onRead">The callback invoked with actual stream read counts.</param>
internal sealed class JsonDocumentReader(Stream source, int maxDepth, Action<int> onRead)
{
    private byte[] _buffer = new byte[4096];
    private int _position;
    private int _length;
    private bool _final;
    private long _offset;
    private long _lineNumber = 1;
    private JsonReaderState _state = new(new JsonReaderOptions { MaxDepth = maxDepth });

    /// <summary>Gets the physical line reached by the parser.</summary>
    public long EndOfFileLineNumber => _lineNumber;

    /// <summary>Reads one complete token, retaining incomplete token bytes across partial reads.</summary>
    /// <param name="cancellationToken">The token used to cancel pending reads.</param>
    /// <returns>The next token, or null after valid end of input.</returns>
    public async ValueTask<JsonDocumentToken?> ReadAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            if (TryRead(out var token))
                return token;

            if (_final)
                return null;

            cancellationToken.ThrowIfCancellationRequested();
            if (_position > 0)
            {
                Buffer.BlockCopy(_buffer, _position, _buffer, 0, _length - _position);
                _length -= _position;
                _position = 0;
            }

            if (_length == _buffer.Length)
                Array.Resize(ref _buffer, checked(_buffer.Length * 2));

            var read = await source.ReadAsync(_buffer.AsMemory(_length), cancellationToken).ConfigureAwait(false);
            onRead(read);
            if (read == 0)
                _final = true;
            else
                _length += read;
        }
    }

    /// <summary>Attempts to advance the BCL reader without carrying its ref-struct state across awaits.</summary>
    /// <param name="token">The parsed token when available.</param>
    /// <returns>Whether a complete token was read.</returns>
    private bool TryRead(out JsonDocumentToken token)
    {
        var span = _buffer.AsSpan(_position, _length - _position);
        var reader = new Utf8JsonReader(span, _final, _state);

        try
        {
            if (reader.Read())
            {
                var tokenStart = checked((int)reader.TokenStartIndex);
                var consumed = checked((int)reader.BytesConsumed);
                var name = reader.TokenType == JsonTokenType.PropertyName ? reader.GetString() : null;
                token = new(reader.TokenType, name, _lineNumber + CountLineFeeds(span[..tokenStart]), _offset + tokenStart, reader.CurrentDepth, _buffer.AsMemory(_position, consumed), tokenStart);
                Advance(reader, span, consumed);
                return true;
            }

            token = default;
            // Preserve incomplete-token prefixes so selected streams retain their original separators.
            if (_final)
                Advance(reader, span, checked((int)reader.BytesConsumed));

            return false;
        }
        catch (InvalidOperationException exception)
        {
            throw new JsonException("A JSON member name could not be decoded.", exception);
        }
    }

    /// <summary>Commits consumed bytes and line state after one reader attempt.</summary>
    private void Advance(Utf8JsonReader reader, ReadOnlySpan<byte> span, int consumed)
    {
        _state = reader.CurrentState;
        _lineNumber += CountLineFeeds(span[..consumed]);
        _offset += consumed;
        _position += consumed;
    }

    /// <summary>Counts physical LF bytes without interpreting JSON string escapes.</summary>
    private static long CountLineFeeds(ReadOnlySpan<byte> bytes)
    {
        long count = 0;
        foreach (var value in bytes)
            if (value == 10)
                count++;

        return count;
    }
}
