namespace CoreEx.Hosting.Work;

/// <summary>
/// Provides the underlying durable <see cref="WorkState"/> persistence with atomic create and optimistic-concurrency (compare-and-set) update semantics.
/// </summary>
/// <remarks>The <see cref="WorkOrchestrator"/> contains all orchestration logic, including transition validation, and relies on the provider for atomicity only. Implementations must guarantee that:
/// <list type="bullet">
/// <item><see cref="CreateAsync"/> is an atomic insert-if-absent; exactly one concurrent caller for the same <see cref="WorkState.OperationKey"/> succeeds.</item>
/// <item><see cref="UpdateAsync"/> and <see cref="DeleteAsync"/> only succeed where the <see cref="WorkState.ETag"/> matches the currently persisted version.</item>
/// <item>A recreated record has a distinct <see cref="WorkState.ETag"/> from every version of its previous incarnation.</item>
/// <item><see cref="GetAsync"/> returns an independent copy with the requested <see cref="WorkState.OperationKey"/> and its <see cref="WorkState.ETag"/> set, and the time observed atomically with the read.</item>
/// <item>A state write uses the immutable identity fields on <see cref="WorkState"/> as its complete logical key; its <see cref="WorkState.Id"/> is only the instance identifier.</item>
/// <item>Records are retained for at least the requested time-to-live; a cache that may evict entries early is <b>not</b> a suitable store.</item>
/// </list>
/// </remarks>
public interface IWorkProvider
{
    /// <summary>
    /// Gets (reads) an independent copy of the <see cref="WorkState"/> using the specified <paramref name="operationKey"/>.
    /// </summary>
    /// <param name="operationKey">The complete operation identity.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="WorkSnapshot"/> containing provider time and the state where found.</returns>
    Task<WorkSnapshot> GetAsync(OperationKey operationKey, CancellationToken cancellationToken);

    /// <summary>
    /// Atomically creates the <paramref name="state"/> where it does not already exist.
    /// </summary>
    /// <param name="state">The <see cref="WorkState"/>; the <see cref="WorkState.ETag"/> is set where successful.</param>
    /// <param name="timeToLive">The minimum time-to-live for the persisted record.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns><see langword="true"/> where created; otherwise, <see langword="false"/> where it already exists.</returns>
    Task<bool> CreateAsync(WorkState state, TimeSpan timeToLive, CancellationToken cancellationToken);

    /// <summary>
    /// Atomically replaces the <paramref name="state"/> where the <see cref="WorkState.ETag"/> matches the persisted version.
    /// </summary>
    /// <param name="state">The <see cref="WorkState"/>; the <see cref="WorkState.ETag"/> is updated where successful.</param>
    /// <param name="timeToLive">The minimum time-to-live for the persisted record.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns><see langword="true"/> where updated; otherwise, <see langword="false"/> where not found or the <see cref="WorkState.ETag"/> does not match.</returns>
    Task<bool> UpdateAsync(WorkState state, TimeSpan timeToLive, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes the <paramref name="state"/> where the <see cref="WorkState.ETag"/> matches the persisted version.
    /// </summary>
    /// <param name="state">The <see cref="WorkState"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns><see langword="true"/> where deleted or no longer exists; otherwise, <see langword="false"/> where the <see cref="WorkState.ETag"/> does not match.</returns>
    Task<bool> DeleteAsync(WorkState state, CancellationToken cancellationToken);

}
