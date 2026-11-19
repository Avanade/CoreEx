namespace CoreEx.Coordination;

/// <summary>
/// Provides type-qualified synchronization keys.
/// </summary>
public static class SynchronizerExtensions
{
    /// <summary>
    /// Attempts to acquire a type-and-name lease.
    /// </summary>
    /// <typeparam name="T">The operation owner type.</typeparam>
    /// <param name="synchronizer">The synchronizer.</param>
    /// <param name="name">The optional operation name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The owned lease, or null for contention.</returns>
    public static Task<ISynchronizationLease?> TryAcquireAsync<T>(this ISynchronizer synchronizer, string? name = null, CancellationToken cancellationToken = default)
        => synchronizer.TryAcquireAsync(JsonSerializer.Serialize(new[] { typeof(T).FullName ?? typeof(T).Name, name }), cancellationToken);
}
