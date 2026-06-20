namespace CoreEx.Entities;

/// <summary>
/// Provides the <see cref="IReadOnlyETag.ETag"/> capabilities.
/// </summary>
public static class ETag
{
    /// <summary>
    /// Compares two <see cref="IReadOnlyETag.ETag"/> values.
    /// </summary>
    /// <param name="etagA">The first entity tag.</param>
    /// <param name="etagB">The second entity tag.</param>
    /// <returns><c>true</c> where the <see cref="IReadOnlyETag.ETag"/> values match; otherwise, <c>false</c>.</returns>
    public static bool TryCompare(IReadOnlyETag? etagA, IReadOnlyETag? etagB) => TryCompare(etagA?.ETag, etagB?.ETag);

    /// <summary>
    /// Compares two ETag <see cref="string"/> values.
    /// </summary>
    /// <param name="etagA">The first entity tag.</param>
    /// <param name="etagB">The second entity tag.</param>
    /// <returns><c>true</c> where the <see cref="IReadOnlyETag.ETag"/> values match; otherwise, <c>false</c>.</returns>
    public static bool TryCompare(string? etagA, string? etagB) => etagA == etagB;

    /// <summary>
    /// Compares two <see cref="IReadOnlyETag.ETag"/> values and throws a <see cref="ConcurrencyException"/> where they do not match.
    /// </summary>
    /// <param name="etagA">The first entity tag.</param>
    /// <param name="etagB">The second entity tag.</param>
    /// <param name="message">The <see cref="Exception.Message"/> override.</param>
    /// <param name="adjuster">The <see cref="ConcurrencyException"/> adjuster.</param>
    public static void Compare(IReadOnlyETag? etagA, IReadOnlyETag? etagB, LText? message = null, Action<ConcurrencyException>? adjuster = null) => Compare(etagA?.ETag, etagB?.ETag, message, adjuster);

    /// <summary>
    /// Compares two ETag <see cref="string"/> values and throws a <see cref="ConcurrencyException"/> where they do not match.
    /// </summary>
    /// <param name="etagA">The first entity tag.</param>
    /// <param name="etagB">The second entity tag.</param>
    /// <param name="message">The <see cref="Exception.Message"/> override.</param>
    /// <param name="adjuster">The <see cref="ConcurrencyException"/> adjuster.</param>
    public static void Compare(string? etagA, string? etagB, LText? message = null, Action<ConcurrencyException>? adjuster = null)
    {
        if (TryCompare(etagA, etagB))
            return;

        var cex = new ConcurrencyException(message);
        adjuster?.Invoke(cex);
        throw cex;
    }

    /// <summary>
    /// Compares two <see cref="IReadOnlyETag.ETag"/> values and returns a <see cref="Result.ConcurrencyError"/> where they do not match.
    /// </summary>
    /// <param name="etagA">The first entity tag.</param>
    /// <param name="etagB">The second entity tag.</param>
    /// <param name="message">The <see cref="Exception.Message"/> override.</param>
    /// <param name="adjuster">The <see cref="ConcurrencyException"/> adjuster.</param>
    /// <returns><see cref="Result.Success"/> where the <see cref="IReadOnlyETag.ETag"/> values match; otherwise, <see cref="Result.ConcurrencyError"/>.</returns>
    public static Result CompareWithResult(IReadOnlyETag? etagA, IReadOnlyETag? etagB, LText? message = null, Action<ConcurrencyException>? adjuster = null) => CompareWithResult(etagA?.ETag, etagB?.ETag, message, adjuster);

    /// <summary>
    /// Compares two ETag <see cref="string"/> values and returns a <see cref="Result.ConcurrencyError"/> where they do not match.
    /// </summary>
    /// <param name="etagA">The first entity tag.</param>
    /// <param name="etagB">The second entity tag.</param>
    /// <param name="message">The <see cref="Exception.Message"/> override.</param>
    /// <param name="adjuster">The <see cref="ConcurrencyException"/> adjuster.</param>
    /// <returns><see cref="Result.Success"/> where the <see cref="IReadOnlyETag.ETag"/> values match; otherwise, <see cref="Result.ConcurrencyError"/>.</returns>
    public static Result CompareWithResult(string? etagA, string? etagB, LText? message = null, Action<ConcurrencyException>? adjuster = null)
    {
        if (TryCompare(etagA, etagB))
            return Result.Success;

        var cex = new ConcurrencyException(message);
        adjuster?.Invoke(cex);
        return cex;
    }

    /// <summary>
    /// Formats a <paramref name="value"/> as an <see cref="IReadOnlyETag.ETag"/> by bookending with the requisite double quotes character; for example '<c>abc</c>' would be formatted as '<c>"abc"</c>'.
    /// </summary>
    /// <param name="value">The value to format.</param>
    /// <returns>The formatted <see cref="IReadOnlyETag.ETag"/>.</returns>
    [return: NotNullIfNotNull(nameof(value))]
    public static string? FormatETag(string? value)
    {
        if (value is null)
            return value;

        if (value.StartsWith('\"') && value.EndsWith('\"'))
            return value;

        if (value.StartsWith("W/\"") && value.EndsWith('\"'))
            return value[2..];

        return $"\"{value}\"";
    }

    /// <summary>
    /// Parses an <see cref="IReadOnlyETag.ETag"/> by removing any weak prefix ('<c>W/</c>') double quotes character bookends; for example '<c>"abc"</c>' would be formatted as '<c>abc</c>'.
    /// </summary>
    /// <param name="etag">The <see cref="IReadOnlyETag.ETag"/> to unformat.</param>
    /// <returns>The unformatted value.</returns>
    [return: NotNullIfNotNull(nameof(etag))]
    public static string? ParseETag(string? etag) => string.IsNullOrEmpty(etag) ? etag : ParseETag(etag.AsSpan());

    /// <summary>
    /// Parses an <see cref="ReadOnlySpan{T}"/> by removing any weak prefix ('<c>W/</c>') double quotes character bookends; for example '<c>"abc"</c>' would be formatted as '<c>abc</c>'.
    /// </summary>
    /// <param name="etag">The <see cref="IReadOnlyETag.ETag"/> to unformat.</param>
    /// <returns>The unformatted value.</returns>
    [return: NotNullIfNotNull(nameof(etag))]
    public static string ParseETag(ReadOnlySpan<char> etag)
    {
        if (etag.IsEmpty)
            return etag.ToString();

        if (etag[0] == '\"' && etag[^1] == '\"')
            return etag[1..^1].ToString();

        if (etag.StartsWith("W/\"") && etag[^1] == '\"')
            return etag[2..^1].ToString();

        return etag.ToString();
    }

    /// <summary>
    /// Generates an ETag for a <paramref name="value"/> by serializing to JSON and using an <see cref="SHA256"/> hash.
    /// </summary>
    /// <typeparam name="T">The <paramref name="value"/> <see cref="Type"/>.</typeparam>
    /// <param name="value">The value.</param>
    /// <param name="jsonSerializerOptions">The optional <see cref="JsonSerializerOptions"/>.</param>
    /// <param name="parts">Additional parts to include in the hash.</param>
    /// <returns>The generated ETag.</returns>
    [return: NotNullIfNotNull(nameof(value))]
    public static string? Generate<T>(T? value, JsonSerializerOptions? jsonSerializerOptions = null, params IEnumerable<string> parts)
    {
        if (value is null)
            return null;

        // Serialize to JSON and then hash.
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value, jsonSerializerOptions ?? JsonDefaults.SerializerOptions);
        byte[] hash;

        if (!parts.Any())
            hash = SHA256.HashData(bytes);
        else
        {
            using var ih = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            ih.AppendData(bytes);
            foreach (var part in parts)
            {
                ih.AppendData(new BinaryData(part));
            }

            hash = ih.GetCurrentHash();
        }

        return ConvertHash(hash);
    }

    /// <summary>
    /// Generates an ETag of the <paramref name="parts"/> using an <see cref="SHA256"/> hash.
    /// </summary>
    /// <param name="parts">The parts to hash.</param>
    /// <returns>The generated ETag.</returns>
    public static string? Generate(params string[] parts)
    {
        if (parts is null || parts.Length == 0)
            return null;

        byte[] hash;

        if (parts.Length == 1)
            hash = SHA256.HashData(new BinaryData(parts[0]));
        else
        {
            using var ih = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            foreach (var part in parts)
            {
                ih.AppendData(new BinaryData(part));
            }

            hash = ih.GetCurrentHash();
        }

        return ConvertHash(hash);
    }

    /// <summary>
    /// Converts the hash into a twelve (12) character string.
    /// </summary>
    private static string ConvertHash(byte[] hash)
    {
        // A hash function produces a fixed-size output — for SHA-256 that’s 256 bits (32 bytes). For an ETag we do not need the whole 256 bits. Hash functions like SHA-256 distribute entropy evenly across all bytes.
        // There is no “hot” region — any subset of bytes is just as random and unique-looking as any other. Therefore, grabbing the first 6 bytes is a perfectly valid way to shorten it.
        return Convert.ToHexString(hash, 0, 6).ToLowerInvariant();
    }
}