namespace CoreEx.Coordination;

/// <summary>
/// Provides type-qualified domain-wide synchronization keys.
/// </summary>
public static class SynchronizerExtensions
{
    /// <summary>
    /// Attempts to acquire a domain-wide type-and-name lease.
    /// </summary>
    /// <typeparam name="T">The operation owner type.</typeparam>
    /// <param name="synchronizer">The synchronizer.</param>
    /// <param name="domainName">The stable domain name.</param>
    /// <param name="name">The optional operation name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The owned lease, or null for contention.</returns>
    /// <remarks>Use the <see cref="ISynchronizer.TryAcquireAsync(OperationKey, CancellationToken)"/> overload for a tenant-scoped identity.</remarks>
    public static Task<ISynchronizationLease?> TryAcquireAsync<T>(this ISynchronizer synchronizer, string domainName, string? name = null, CancellationToken cancellationToken = default)
        => synchronizer.ThrowIfNull().TryAcquireAsync(OperationKey.ForDomain(domainName, typeof(T).FullName ?? typeof(T).Name, name is null ? "default" : $"name:{name}"), cancellationToken);
}
