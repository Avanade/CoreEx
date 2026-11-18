namespace CoreEx.IO.JsonLines;

/// <summary>Carries one actual source-byte span, independent of buffered stream read positions.</summary>
/// <param name="Bytes">The line content bytes, excluding its line terminator.</param>
/// <param name="Line">The physical line number.</param>
/// <param name="Offset">The source offset where the line begins.</param>
internal sealed record JsonLinesFrame(ReadOnlyMemory<byte> Bytes, long Line, long Offset);
