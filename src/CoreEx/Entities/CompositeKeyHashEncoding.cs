namespace CoreEx.Entities;

/// <summary>
/// Specifies a textual encoding for a canonical composite-key hash.
/// </summary>
public enum CompositeKeyHashEncoding
{
    /// <summary>
    /// Lowercase hexadecimal encoding.
    /// </summary>
    Hex,

    /// <summary>
    /// Unpadded URL-safe Base64 encoding.
    /// </summary>
    Base64Url,

    /// <summary>
    /// Fixed-width Base58 encoding using the Bitcoin alphabet.
    /// </summary>
    Base58
}
