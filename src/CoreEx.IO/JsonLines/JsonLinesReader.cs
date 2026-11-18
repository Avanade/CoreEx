namespace CoreEx.IO.JsonLines;

/// <summary>Frames LF/CRLF lines while preserving actual byte offsets independently of stream read-ahead.</summary>
/// <param name="source">The input stream to read.</param>
/// <param name="onRead">The callback invoked with each actual stream read count.</param>
internal sealed class JsonLinesReader(Stream source, Action<int> onRead)
{
    private readonly byte[] _buffer = new byte[4096];
    private readonly System.Buffers.ArrayBufferWriter<byte> _line = new();
    private int _position;
    private int _length;
    private long _offset;
    private long _lineNumber;

    /// <summary>Reads one line without treating a final separator as an additional empty line.</summary>
    /// <param name="cancellationToken">The token used to cancel pending reads.</param>
    /// <returns>The next framed line, or null at end of input.</returns>
    public async Task<JsonLinesFrame?> ReadAsync(CancellationToken cancellationToken)
    {
        _line.Clear();
        var start = _offset;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_position == _length)
            {
                _length = await source.ReadAsync(_buffer, cancellationToken).ConfigureAwait(false);
                _position = 0;
                onRead(_length);
                if (_length == 0)
                    return _line.WrittenCount == 0 ? null : new(_line.WrittenMemory, ++_lineNumber, start);
            }

            var newline = _buffer.AsSpan(_position, _length - _position).IndexOf((byte)10);
            var count = newline < 0 ? _length - _position : newline;
            _buffer.AsSpan(_position, count).CopyTo(_line.GetSpan(count));
            _line.Advance(count);
            _position += count;
            _offset += count;

            if (newline >= 0)
            {
                _position++;
                _offset++;
                var content = _line.WrittenMemory;
                if (content.Length > 0 && content.Span[^1] == 13)
                    content = content[..^1];

                return new(content, ++_lineNumber, start);
            }
        }
    }
}
