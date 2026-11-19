namespace CoreEx.Coordination;

/// <summary>
/// Acquires bounded, owned leases to coordinate execution.
/// </summary>
/// <remarks>Scope and failure guarantees depend on the provider. Lease loss cannot prevent uncooperative or paused code from continuing; correctness-critical resources require fencing or transactional ownership checks.</remarks>
public interface ISynchronizer
{
    /// <summary>
    /// Attempts to acquire a lease for the logical key.
    /// </summary>
    /// <param name="key">The logical key, qualified by the registered key provider.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>An owned lease that the caller must dispose, normally with <c>await using</c>, or null for contention. Backend failures propagate.</returns>
    /// <remarks>The synchronizer is a reusable acquisition service. Each returned lease owns its own renewal and release lifetime; disposing the service is not a substitute for disposing the lease.</remarks>
    Task<ISynchronizationLease?> TryAcquireAsync(string key, CancellationToken cancellationToken = default);
}
