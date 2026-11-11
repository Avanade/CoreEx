namespace CoreEx.IO.Json;

/// <summary>Contains one parsed token and its exact consumed source span.</summary>
/// <param name="Type">The token kind.</param>
/// <param name="Name">The decoded object member name, when the token is a property name.</param>
/// <param name="LineNumber">The one-based physical line containing the token.</param>
/// <param name="ByteOffset">The zero-based source offset where the token begins.</param>
/// <param name="Depth">The parser depth associated with the token.</param>
/// <param name="Raw">The bytes consumed to reach the token, including intervening separators and whitespace.</param>
/// <param name="TokenStart">The offset of the token within the consumed source bytes.</param>
internal readonly record struct JsonDocumentToken(JsonTokenType Type, string? Name, long LineNumber, long ByteOffset, int Depth, ReadOnlyMemory<byte> Raw, int TokenStart);
