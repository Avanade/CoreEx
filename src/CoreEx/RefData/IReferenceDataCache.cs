namespace CoreEx.RefData;

/// <summary>
/// Enables caching of reference data in a implementation-agnostic manner.
/// </summary>
public interface IReferenceDataCache
{
    /// <summary>
    /// Gets the <see cref="IReferenceDataCollection"/> for the specified <paramref name="type"/> where it exists; otherwise, invokes the <paramref name="factory"/> to create.
    /// </summary>
    /// <param name="type">The <see cref="IReferenceData"/> <see cref="Type"/>.</param>
    /// <param name="factory">The factory to create where the <see cref="IReferenceDataCollection"/> is not in the cache.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="IReferenceDataCollection"/> from the cache.</returns>
    Task<IReferenceDataCollection> GetOrCreateAsync(Type type, Func<Type, CancellationToken, Task<IReferenceDataCollection>> factory, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes the <see cref="IReferenceDataCollection"/> for the specified <paramref name="type"/> from the cache.
    /// </summary>
    /// <param name="type">The <see cref="IReferenceData"/> <see cref="Type"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    Task RemoveAsync(Type type, CancellationToken cancellationToken = default);
}
