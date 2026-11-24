namespace CoreEx.Coordination;

/// <summary>
/// Provides type-qualified synchronization extensions.
/// </summary>
public static class SynchronizerExtensions
{
    /// <summary>
    /// Attempts to acquire a type-and-name lease.
    /// </summary>
    /// <typeparam name="T">The operation owner type; its <see cref="Type.FullName"/> is the operation name.</typeparam>
    /// <param name="synchronizer">The synchronizer.</param>
    /// <param name="name">The optional instance name; <see langword="null"/> selects the unnamed lease, distinct from a lease named <c>default</c>.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The owned lease, or null for contention.</returns>
    /// <remarks>The lease is tenant-scoped where an ambient <see cref="ExecutionContext.TenantId"/> is present; otherwise, domain-wide.</remarks>
    public static Task<ISynchronizationLease?> TryAcquireAsync<T>(this ISynchronizer synchronizer, string? name = null, CancellationToken cancellationToken = default)
        => synchronizer.ThrowIfNull().TryAcquireAsync(typeof(T).FullName ?? typeof(T).Name, CompositeKey.Create(name is null ? "default" : $"name:{name}"), cancellationToken);
}
