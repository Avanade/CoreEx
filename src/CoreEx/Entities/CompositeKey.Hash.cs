namespace CoreEx.Entities;

public readonly partial struct CompositeKey
{
    private static readonly UTF8Encoding _hashEncoding = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
    private const string Base58Alphabet = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";

    /// <summary>
    /// Gets the current <see cref="ToCanonicalHash()"/> format version.
    /// </summary>
    /// <remarks>Persisted hashes depend on this format; any change requires a new version and a migration strategy.</remarks>
    public const byte CanonicalHashVersion = 1;

    /// <summary>
    /// Gets the <see cref="ToCanonicalHash()"/> length in bytes.
    /// </summary>
    public const int CanonicalHashLength = 32;

    /// <summary>
    /// Gets the fixed-length SHA-256 hash of the key, suitable for use as a persisted identity.
    /// </summary>
    /// <returns>The <see cref="CanonicalHashLength"/>-byte hash.</returns>
    /// <remarks>The hash input is the <see cref="CanonicalHashVersion"/> byte followed, for each component, by a presence byte (<c>0</c> where <see langword="null"/>; otherwise <c>1</c>, a 4-byte big-endian length and the strict UTF-8 encoding of the same invariant value text used by <see cref="ToCanonicalString"/>).
    /// <para>Framing makes every component sequence unambiguous, so unlike <see cref="ToCanonicalString"/> a <see langword="null"/> component is permitted (and distinct from an empty string), and a component of a multi-component key may contain <see cref="Delimiter"/> or a NUL character. Comparison is exact and ordinal, including case and trailing spaces. Type is not part of identity. This is not a secret; verify the located identity where a collision would matter.</para></remarks>
    /// <exception cref="ArgumentException">The key has no components, or a component contains an unpaired surrogate.</exception>
    /// <exception cref="NotSupportedException">An argument has an unsupported type.</exception>
    public byte[] ToCanonicalHash() => ToCanonicalHash(CanonicalHashLength);

    /// <summary>
    /// Gets the SHA-256 hash of the key, optionally truncated to the requested length.
    /// </summary>
    /// <param name="length">The number of leading hash bytes to return, from 16 through <see cref="CanonicalHashLength"/>.</param>
    /// <returns>The requested number of leading hash bytes.</returns>
    /// <remarks>Truncation reduces collision resistance; use the full hash unless a shorter identity is required.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="length"/> is less than 16 or greater than <see cref="CanonicalHashLength"/>.</exception>
    /// <exception cref="ArgumentException">The key has no components, or a component contains an unpaired surrogate.</exception>
    /// <exception cref="NotSupportedException">An argument has an unsupported type.</exception>
    public byte[] ToCanonicalHash(int length)
    {
        if (length < 16 || length > CanonicalHashLength)
            throw new ArgumentOutOfRangeException(nameof(length), length, $"Canonical hashes must retain between 16 and {CanonicalHashLength} bytes.");

        var args = Args;
        if (args.IsDefaultOrEmpty)
            throw new ArgumentException("A canonical composite key requires at least one component.", "key");

        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        hash.AppendData([CanonicalHashVersion]);

        Span<byte> componentLength = stackalloc byte[4];
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] is null)
            {
                hash.AppendData([0]);
                continue;
            }

            byte[] bytes;
            try
            {
                bytes = _hashEncoding.GetBytes(ArgumentToString(args[i], strict: true));
            }
            catch (EncoderFallbackException ex)
            {
                throw new ArgumentException($"A canonical composite key component must not contain an unpaired surrogate (index {i}).", "key", ex);
            }

            BinaryPrimitives.WriteInt32BigEndian(componentLength, bytes.Length);
            hash.AppendData([1]);
            hash.AppendData(componentLength);
            hash.AppendData(bytes);
        }

        var result = hash.GetHashAndReset();
        return length == result.Length ? result : result[..length];
    }

    /// <summary>
    /// Gets the <see cref="ToCanonicalHash()"/> as a lowercase hexadecimal string.
    /// </summary>
    /// <returns>The 64-character lowercase hexadecimal hash.</returns>
    /// <remarks>The result contains only <c>0-9</c> and <c>a-f</c>, so it is safe as a URL segment, document identifier or cache key.</remarks>
    /// <exception cref="ArgumentException">The key has no components, or a component contains an unpaired surrogate.</exception>
    /// <exception cref="NotSupportedException">An argument has an unsupported type.</exception>
    public string ToCanonicalHashString() => ToCanonicalHashString(CompositeKeyHashEncoding.Hex, CanonicalHashLength);

    /// <summary>
    /// Gets the <see cref="ToCanonicalHash()"/> encoded as a string.
    /// </summary>
    /// <param name="encoding">The text encoding to use.</param>
    /// <param name="length">The number of leading hash bytes to encode, from 16 through <see cref="CanonicalHashLength"/>.</param>
    /// <returns>The encoded hash string.</returns>
    /// <remarks>The default encoding is lowercase hexadecimal. Base64url is unpadded. Base58 is left-padded with <c>1</c> to a fixed width for the selected byte length.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="length"/> is less than 16 or greater than <see cref="CanonicalHashLength"/>, or <paramref name="encoding"/> is not defined.</exception>
    /// <exception cref="ArgumentException">The key has no components, or a component contains an unpaired surrogate.</exception>
    /// <exception cref="NotSupportedException">An argument has an unsupported type.</exception>
    public string ToCanonicalHashString(CompositeKeyHashEncoding encoding, int length = CanonicalHashLength)
    {
        var hash = ToCanonicalHash(length);
        return encoding switch
        {
            CompositeKeyHashEncoding.Hex => Convert.ToHexString(hash).ToLowerInvariant(),
            CompositeKeyHashEncoding.Base64Url => Convert.ToBase64String(hash).TrimEnd('=').Replace('+', '-').Replace('/', '_'),
            CompositeKeyHashEncoding.Base58 => EncodeBase58(hash),
            _ => throw new ArgumentOutOfRangeException(nameof(encoding), encoding, "The canonical hash encoding is not defined.")
        };
    }

    /// <summary>
    /// Encodes the hash as fixed-width Base58.
    /// </summary>
    private static string EncodeBase58(byte[] hash)
    {
        var value = new System.Numerics.BigInteger(hash, isUnsigned: true, isBigEndian: true);
        var width = (int)Math.Ceiling(hash.Length * 8d / Math.Log2(Base58Alphabet.Length));
        var buffer = new char[width];
        var position = buffer.Length;

        while (value > System.Numerics.BigInteger.Zero)
        {
            value = System.Numerics.BigInteger.DivRem(value, 58, out var remainder);
            buffer[--position] = Base58Alphabet[(int)remainder];
        }

        while (position > 0)
            buffer[--position] = Base58Alphabet[0];

        return new string(buffer);
    }
}
