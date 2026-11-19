namespace CoreEx.Hosting.Work;

/// <summary>
/// Provides the underlying durable <see cref="WorkState"/> persistence with atomic create and optimistic-concurrency (compare-and-set) update semantics.
/// </summary>
/// <remarks>The <see cref="WorkOrchestrator"/> contains all orchestration logic, including transition validation, and relies on the provider for atomicity only. Implementations must guarantee that:
/// <list type="bullet">
/// <item><see cref="CreateAsync"/> is an atomic insert-if-absent; exactly one concurrent caller for the same identifier succeeds.</item>
/// <item><see cref="UpdateAsync"/> and <see cref="DeleteAsync"/> only succeed where the <see cref="WorkState.ETag"/> matches the currently persisted version; the state and any supplied data are written atomically.</item>
/// <item>A recreated record has a distinct <see cref="WorkState.ETag"/> from every version of its previous incarnation.</item>
/// <item><see cref="GetAsync"/> returns an independent copy (mutating it has no effect until updated) with the <see cref="WorkState.ETag"/> set.</item>
/// <item>Records are retained for at least the requested time-to-live; a cache that may evict entries early is <b>not</b> a suitable store.</item>
/// </list>
/// <para>The result data enables simple (smallish) persistence; large data should be persisted independently with the result data here representing a link to a file/blob for example.</para></remarks>
public interface IWorkProvider
{
    /// <summary>
    /// Gets (reads) an independent copy of the <see cref="WorkState"/> using the specified <paramref name="id"/>.
    /// </summary>
    /// <param name="id">The <see cref="WorkState.Id"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="WorkState"/> (with the <see cref="WorkState.ETag"/> set) where found; otherwise, <see langword="null"/>.</returns>
    Task<WorkState?> GetAsync(string id, CancellationToken cancellationToken);

    /// <summary>
    /// Atomically creates the <paramref name="state"/> where it does not already exist.
    /// </summary>
    /// <param name="state">The <see cref="WorkState"/>; the <see cref="WorkState.ETag"/> is set where successful.</param>
    /// <param name="timeToLive">The minimum time-to-live for the persisted record.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns><see langword="true"/> where created; otherwise, <see langword="false"/> where it already exists.</returns>
    Task<bool> CreateAsync(WorkState state, TimeSpan timeToLive, CancellationToken cancellationToken);

    /// <summary>
    /// Atomically replaces the <paramref name="state"/> (and optionally the <paramref name="data"/>) where the <see cref="WorkState.ETag"/> matches the persisted version.
    /// </summary>
    /// <param name="state">The <see cref="WorkState"/>; the <see cref="WorkState.ETag"/> is updated where successful.</param>
    /// <param name="data">The result data to persist; <see langword="null"/> leaves any existing data unchanged.</param>
    /// <param name="timeToLive">The minimum time-to-live for the persisted record.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns><see langword="true"/> where updated; otherwise, <see langword="false"/> where not found or the <see cref="WorkState.ETag"/> does not match.</returns>
    Task<bool> UpdateAsync(WorkState state, BinaryData? data, TimeSpan timeToLive, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes the <paramref name="state"/> (and data) where the <see cref="WorkState.ETag"/> matches the persisted version.
    /// </summary>
    /// <param name="state">The <see cref="WorkState"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns><see langword="true"/> where deleted or no longer exists; otherwise, <see langword="false"/> where the <see cref="WorkState.ETag"/> does not match.</returns>
    Task<bool> DeleteAsync(WorkState state, CancellationToken cancellationToken);

    /// <summary>
    /// Gets the result data using the specified <paramref name="id"/>.
    /// </summary>
    /// <param name="id">The <see cref="WorkState.Id"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="BinaryData"/> where found; otherwise, <see langword="null"/>.</returns>
    Task<BinaryData?> GetDataAsync(string id, CancellationToken cancellationToken);
}
