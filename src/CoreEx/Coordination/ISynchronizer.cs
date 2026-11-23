namespace CoreEx.Coordination;

/// <summary>
/// Acquires bounded, owned leases to coordinate execution.
/// </summary>
/// <remarks>Scope and failure guarantees depend on the provider. Lease loss cannot prevent uncooperative or paused code from continuing; correctness-critical resources require fencing or transactional ownership checks.</remarks>
public interface ISynchronizer
{
    /// <summary>
    /// Attempts to acquire a lease for the named, keyed operation.
    /// </summary>
    /// <param name="name">The stable application-defined operation name.</param>
    /// <param name="key">The operation instance key; its canonical string must not be empty or whitespace.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>An owned lease that the caller must dispose, or null for contention.</returns>
    /// <remarks>The provider qualifies the identity with the host <see cref="IHostSettings.DomainName"/> and, where present, the ambient <see cref="ExecutionContext.TenantId"/>; without a tenant the lease is domain-wide. Backend failures propagate. The synchronizer is a reusable acquisition service; each returned lease owns its own renewal and release lifetime.</remarks>
    Task<ISynchronizationLease?> TryAcquireAsync(string name, CompositeKey key, CancellationToken cancellationToken = default);
}
