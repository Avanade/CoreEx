namespace CoreEx.Data;

/// <summary>
/// Enables a read-only time-to-live (TTL) capability.
/// </summary>
public interface IReadOnlyTimeToLive
{
    /// <summary>
    /// Gets the number of seconds until the item expires.
    /// </summary>
    /// <remarks>A <see langword="null"/> value indicates that the item will not expire via this mechanism. The exact expiry semantics (e.g. relative to last-modified time, whether <c>-1</c>/<c>0</c> carry special
    /// meaning, whether a store-level default applies where unspecified) are store-specific — see the implementing store's own documentation for the precise behaviour.
    /// <para>This is a <i>relative</i> seconds value, chosen because it maps directly onto Cosmos DB's reserved <c>ttl</c> system property (also relative — re-evaluated from the document's last-modified time on
    /// every write). It is <b>not</b> a direct field-level fit for MongoDB's TTL-index mechanism, which requires an indexed <c>Date</c> field: a per-document variable expiry in MongoDB needs an <i>absolute</i>
    /// expiry instant (an index with <c>expireAfterSeconds: 0</c> over a `Date` field holding that instant), not a relative seconds count. A MongoDB implementation of this interface is expected to translate
    /// <see cref="TimeToLive"/> into an absolute expiry (e.g. <c>DateTime.UtcNow.AddSeconds(value)</c>) at write time and persist that into whatever field its TTL index targets, rather than storing the relative
    /// value verbatim — the same kind of per-store reinterpretation already expected of <see cref="IPartitionKey"/> (a single opaque value with a different physical meaning per store).</para></remarks>
    int? TimeToLive { get; }
}
