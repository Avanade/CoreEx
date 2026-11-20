namespace CoreEx.Coordination;

/// <summary>
/// Holds atomic in-memory coordination state shared across scopes of one root service provider.
/// </summary>
public sealed class InMemoryCoordinationState
{
    /// <summary>
    /// Gets the synchronization object for coordinating access to the state.
    /// </summary>
#if NET8_0
    internal readonly object SyncRoot = new();
#else
    internal readonly Lock SyncRoot = new();
#endif

    /// <summary>
    /// Gets the entries dictionary.
    /// </summary>
    internal readonly Dictionary<(string Purpose, OperationKey Key), Entry> Entries = [];

    /// <summary>
    /// Represents one retained ownership record.
    /// </summary>
    internal sealed class Entry
    {
        /// <summary>Gets the owner revision.</summary>
        public required string Owner { get; init; }

        /// <summary>Gets the optional idempotency fingerprint.</summary>
        public string? Fingerprint { get; init; }

        /// <summary>Gets or sets the ownership deadline.</summary>
        public DateTimeOffset Deadline { get; set; }

        /// <summary>Gets or sets the record expiry.</summary>
        public DateTimeOffset Expiry { get; set; }

        /// <summary>Gets or sets the processing state.</summary>
        public CoreEx.Idempotency.IdempotencyOutcome State { get; set; }

        /// <summary>Gets or sets the terminal response.</summary>
        public BinaryData? Response { get; set; }
    }
}
