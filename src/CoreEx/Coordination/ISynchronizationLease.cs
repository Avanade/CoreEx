namespace CoreEx.Coordination;

/// <summary>
/// Represents automatically renewed ownership and its per-acquisition lifetime.
/// </summary>
/// <remarks>Always dispose the handle, normally with <c>await using</c>. Disposal stops renewal and performs the purpose-specific owner-checked cleanup; synchronization leases release their claim, whereas idempotency attempts retain their outcome. Disposing an acquisition must never release a different owner.</remarks>
public interface ISynchronizationLease : IAsyncDisposable
{
    /// <summary>
    /// Gets the token cancelled irreversibly when ownership can no longer be established.
    /// </summary>
    CancellationToken OwnershipLost { get; }
}
