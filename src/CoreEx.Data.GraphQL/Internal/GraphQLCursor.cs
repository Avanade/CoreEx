namespace CoreEx.Data.GraphQL.Internal;

/// <summary>
/// Encodes/decodes the opaque Relay <see href="https://relay.dev/graphql/connections.htm">Cursor Connections</see> cursor used by query root pagination, backed by the
/// underlying offset-based <see cref="PagingArgs"/>.
/// </summary>
/// <remarks>Uses a simpler <c>offset:{offset}</c> convention (rather than Relay's own <c>arrayconnection:{offset}</c>) for offset-based (non-keyset) data sources: the cursor is
/// an opaque, base64-encoded absolute row offset. Client GraphQL libraries (Apollo Client, Relay) never inspect cursor contents — they only echo them back verbatim — so this
/// encoding is fully interoperable despite not being a true keyset cursor and not matching Relay's own prefix.</remarks>
internal static class GraphQLCursor
{
    private const string Prefix = "offset:";

    /// <summary>
    /// Encodes the specified absolute row <paramref name="offset"/> as an opaque cursor string.
    /// </summary>
    /// <param name="offset">The zero-based absolute row offset.</param>
    /// <returns>The opaque, base64-encoded cursor string.</returns>
    public static string Encode(int offset) => Convert.ToBase64String(Encoding.UTF8.GetBytes($"{Prefix}{offset.ToString(CultureInfo.InvariantCulture)}"));

    /// <summary>
    /// Attempts to decode the specified opaque <paramref name="cursor"/> back to its absolute row offset.
    /// </summary>
    /// <param name="cursor">The opaque cursor string (as previously produced by <see cref="Encode(int)"/>).</param>
    /// <param name="offset">The decoded zero-based absolute row offset.</param>
    /// <returns><see langword="true"/> where <paramref name="cursor"/> was successfully decoded; otherwise, <see langword="false"/>.</returns>
    public static bool TryDecode(string cursor, out int offset)
    {
        offset = 0;

        try
        {
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
            return decoded.StartsWith(Prefix, StringComparison.Ordinal) && int.TryParse(decoded.AsSpan(Prefix.Length), NumberStyles.Integer, CultureInfo.InvariantCulture, out offset) && offset >= 0;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
