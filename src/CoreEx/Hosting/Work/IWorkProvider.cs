namespace CoreEx.Hosting.Work;

/// <summary>
/// Provides the underlying <see cref="WorkState"/> implementation services.
/// </summary>
/// <remarks>The <see cref="WorkOrchestrator"/> contains all orchestration logic, including validation, etc. which is performed prior to invoking the <see cref="IWorkProvider"/>. Therefore, there is no need
/// to repeat within the provider implementation; i.e. simply provide the persistence as requested.
/// <para>The <see cref="GetDataAsync"/> and <see cref="SetDataAsync"/> enable simple (smallish) result data persistence; large data should be persisted independently with the result data here representing a link
/// to a file/blob for example.</para></remarks>
public interface IWorkProvider
{
    /// <summary>
    /// Gets (reads) the <see cref="WorkState"/> from the persistence store using the specified <paramref name="id"/>.
    /// </summary>
    /// <param name="id">The <see cref="WorkState.Id"/>.</param>
    /// <returns>The <see cref="WorkState"/> where found; otherwise, <c>null</c>.</returns>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    Task<WorkState?> GetAsync(string id, CancellationToken cancellationToken);

    /// <summary>
    /// Creates (saves) the <paramref name="state"/> to the persistence store as new.
    /// </summary>
    /// <param name="state">The <see cref="WorkState"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    Task CreateAsync(WorkState state, CancellationToken cancellationToken);

    /// <summary>
    /// Updates (saves) the <paramref name="state"/> to the persistence store replacing existing.
    /// </summary>
    /// <param name="state">The <see cref="WorkState"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    Task UpdateAsync(WorkState state, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes the <see cref="WorkState"/> from the persistence store using the specified <paramref name="id"/>.
    /// </summary>
    /// <param name="id">The <see cref="WorkState.Id"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <remarks>A delete should be considered idempotent and therefore should not fail where not found.</remarks>
    Task DeleteAsync(string id, CancellationToken cancellationToken);

    /// <summary>
    /// Gets the <see cref="WorkState"/> <see cref="BinaryData"/> using the specified <paramref name="id"/>.
    /// </summary>
    /// <param name="id">The <see cref="WorkState.Id"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="BinaryData"/> where found; otherwise, <c>null</c>.</returns>
    Task<BinaryData?> GetDataAsync(string id, CancellationToken cancellationToken);

    /// <summary>
    /// Gets the <see cref="WorkState"/> <see cref="BinaryData"/> for the specified <paramref name="id"/>.
    /// </summary>
    /// <param name="id">The <see cref="WorkState.Id"/>.</param>
    /// <param name="data">The <see cref="BinaryData"/> to persist.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    Task SetDataAsync(string id, BinaryData data, CancellationToken cancellationToken);
}