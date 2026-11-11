namespace CoreEx.IO.Json;

/// <summary>Describes a token's pointer, traversed container, and duplicate status.</summary>
/// <param name="Path">The current value or containing object pointer.</param>
/// <param name="ObjectPath">Whether selector segments traversed object members only.</param>
/// <param name="ParentType">The parent container kind.</param>
/// <param name="ParentPath">The parent container pointer.</param>
/// <param name="Depth">The current nesting depth.</param>
/// <param name="DuplicateMember">Whether the current property name was already present in its object.</param>
/// <param name="IsValue">Whether the token begins or contains a JSON value.</param>
internal readonly record struct JsonDocumentPosition(string Path, bool ObjectPath, JsonTokenType? ParentType, string? ParentPath, int Depth, bool DuplicateMember, bool IsValue);
