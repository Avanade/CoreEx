namespace CoreEx.IO.Json;

/// <summary>Advances the shared document reader and validates paths even when mappings skip content.</summary>
/// <param name="reader">The incremental source reader.</param>
/// <param name="report">Reports parser and duplicate-member failures without exposing source values.</param>
/// <param name="stopped">Gets whether the invocation has stopped.</param>
/// <param name="invalidValue">Gets whether a structural error invalidated the current selected object.</param>
internal sealed class JsonDocumentCursor(JsonDocumentReader reader, Func<long?, string, CancellationToken, Task> report, Func<bool> stopped, Func<bool> invalidValue)
{
    private readonly JsonDocumentPathTracker _paths = new();

    /// <summary>Gets the current token, whose bytes are borrowed until the next read.</summary>
    public JsonDocumentToken Token { get; private set; }

    /// <summary>Gets the current decoded pointer and container position.</summary>
    public JsonDocumentPosition Position { get; private set; }

    /// <summary>Gets whether malformed syntax ended traversal.</summary>
    public bool Failed { get; private set; }

    /// <summary>Gets whether the current selected object has already failed structural validation.</summary>
    public bool InvalidValue => invalidValue();

    /// <summary>Reads the next token, including syntax and duplicate checks for skipped values.</summary>
    public async ValueTask<bool> ReadAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (stopped() || Failed)
            return false;

        JsonDocumentToken? next;
        try
        {
            next = await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (JsonException exception)
        {
            Failed = true;
            await report(exception.LineNumber + 1, Position.Path ?? string.Empty, cancellationToken).ConfigureAwait(false);
            return false;
        }

        if (next is null)
            return false;

        Token = next.Value;
        Position = _paths.Advance(Token);
        if (Position.DuplicateMember)
            await report(Token.LineNumber, Position.Path, cancellationToken).ConfigureAwait(false);

        return !stopped();
    }

    /// <summary>Consumes the current value without capturing bytes, while retaining structural checks.</summary>
    public async ValueTask SkipAsync(CancellationToken cancellationToken)
    {
        if (Token.Type is not (JsonTokenType.StartObject or JsonTokenType.StartArray))
            return;

        await CompleteContainerAsync(Token.Depth, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Finishes an open container without mapping or retaining its remaining content.</summary>
    public async ValueTask CompleteContainerAsync(int depth, CancellationToken cancellationToken)
    {
        while (await ReadAsync(cancellationToken).ConfigureAwait(false))
            if (Token.Type is JsonTokenType.EndObject or JsonTokenType.EndArray && Token.Depth == depth)
                return;
    }
}
