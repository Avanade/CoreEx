namespace CoreEx.Hosting.Synchronization;

/// <summary>
/// Enables concurrency management to synchronize the underlying execution.
/// </summary>
/// <remarks>The <see cref="EnterAsync{T}"/> must acquire and hold a lock until the corresponding <see cref="ExitAsync"/> is invoked. Where a lock is unable to be acquired then a <see langword="false"/> must be returned to advise the caller
/// that processing can not occur at this time as another process is currently executing. A result of <see langword="true"/> indicates the lock was acquired and will be held until the corresponding <see cref="ExitAsync"/>.</remarks>
public interface ISynchronizer : IAsyncDisposable
{
    /// <summary>
    /// Acquires a lock on the specified <typeparamref name="T"/> and optional <paramref name="name"/>.
    /// </summary>
    /// <typeparam name="T">The <see cref="Type"/> to lock.</typeparam>
    /// <param name="name">The optional name to differentiate the lock.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns><see langword="true"/> where the lock is aquired; otherwise,<see langword="false"/>.</returns>
    Task<bool> EnterAsync<T>(string? name = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Releases the lock on the specified <typeparamref name="T"/> and optional <paramref name="name"/>.
    /// </summary>
    /// <typeparam name="T">The <see cref="Type"/> to unlock.</typeparam>
    /// <param name="name">The optional name to differentiate the lock.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    Task ExitAsync<T>(string? name = null, CancellationToken cancellationToken = default);
}