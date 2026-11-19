namespace CoreEx.Http;

/// <summary>
/// Formats logical idempotency keys as standard Structured Field strings.
/// </summary>
internal static class IdempotencyKeyHeader
{
    /// <summary>
    /// Formats a logical key as one RFC 9651 string Item.
    /// </summary>
    /// <param name="key">The logical key.</param>
    /// <returns>The quoted header value.</returns>
    public static string Format(string key)
    {
        key.ThrowIfNull();
        if (key.Length is < 8 or > 128 || key.Any(character => character is < ' ' or > '~'))
            throw new ArgumentException("The idempotency key must be 8-128 visible ASCII characters.", nameof(key));

        var builder = new StringBuilder(key.Length + 2);
        builder.Append('"');
        foreach (var character in key)
        {
            if (character is '"' or '\\')
                builder.Append('\\');

            builder.Append(character);
        }

        return builder.Append('"').ToString();
    }
}
