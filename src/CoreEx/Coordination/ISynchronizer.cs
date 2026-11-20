namespace CoreEx.Coordination;

/// <summary>
/// Acquires bounded, owned leases to coordinate execution.
/// </summary>
/// <remarks>Scope and failure guarantees depend on the provider. Lease loss cannot prevent uncooperative or paused code from continuing; correctness-critical resources require fencing or transactional ownership checks.</remarks>
public interface ISynchronizer
{
    /// <summary>
    /// Attempts to acquire a lease for an explicitly scoped operation.
    /// </summary>
    /// <param name="key">The complete operation identity.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>An owned lease that the caller must dispose, or null for contention.</returns>
    /// <remarks>The key is used as supplied and must not be re-qualified from ambient context. Backend failures propagate. The synchronizer is a reusable acquisition service; each returned lease owns its own renewal and release lifetime.</remarks>
    Task<ISynchronizationLease?> TryAcquireAsync(OperationKey key, CancellationToken cancellationToken = default);
}
