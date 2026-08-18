namespace CoreEx.Data;

/// <summary>
/// Enables a mutable time-to-live (TTL) capability.
/// </summary>
public interface ITimeToLive : IReadOnlyTimeToLive
{
    /// <inheritdoc/>
    int? IReadOnlyTimeToLive.TimeToLive => TimeToLive;

    /// <summary>
    /// Gets or sets the number of seconds until the item expires.
    /// </summary>
    /// <remarks>See <see cref="IReadOnlyTimeToLive.TimeToLive"/> for expiry semantics, which are store-specific.</remarks>
    new int? TimeToLive { get; set; }
}
