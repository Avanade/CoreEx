namespace CoreEx.Database;

public abstract partial class DatabaseCommand
{
    /// <summary>
    /// Selects none or more items from the first result set.
    /// </summary>
    /// <typeparam name="T">The item <see cref="Type"/>.</typeparam>
    /// <param name="func">The <see cref="DatabaseRecord"/> mapping function.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The resulting set.</returns>
    public Task<IEnumerable<T>> SelectQueryAsync<T>(Func<DatabaseRecord, OperationType, T> func, CancellationToken cancellationToken = default)
        => SelectQueryAsync(new DatabaseMapper<T>(func.ThrowIfNull()), cancellationToken);

    /// <summary>
    /// Selects none or more items from the first result set using a <paramref name="mapper"/>.
    /// </summary>
    /// <typeparam name="T">The item <see cref="Type"/>.</typeparam>
    /// <param name="mapper">The <see cref="IDatabaseMapper{T}"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The item sequence.</returns>
    public async Task<IEnumerable<T>> SelectQueryAsync<T>(IDatabaseMapper<T> mapper, CancellationToken cancellationToken = default)
    {
        var coll = new List<T>();
        await SelectQueryAsync(coll, mapper, cancellationToken).ConfigureAwait(false);
        return coll;
    }

    /// <summary>
    /// Selects none or more items from the first result set and adds to the <paramref name="collection"/>.
    /// </summary>
    /// <typeparam name="T">The item <see cref="Type"/>.</typeparam>
    /// <typeparam name="TColl">The collection <see cref="Type"/>.</typeparam>
    /// <param name="collection">The collection.</param>
    /// <param name="func">The <see cref="DatabaseRecord"/> mapping function.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    public Task SelectQueryAsync<T, TColl>(TColl collection, Func<DatabaseRecord, OperationType, T> func, CancellationToken cancellationToken = default) where TColl : ICollection<T>
        => SelectQueryAsync(collection, new DatabaseMapper<T>(func), cancellationToken);

    /// <summary>
    /// Selects none or more items from the first result set and adds to the <paramref name="collection"/>.
    /// </summary>
    /// <typeparam name="T">The item <see cref="Type"/>.</typeparam>
    /// <typeparam name="TColl">The collection <see cref="Type"/>.</typeparam>
    /// <param name="collection">The collection.</param>
    /// <param name="mapper">The <see cref="IDatabaseMapper{T}"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    public Task SelectQueryAsync<T, TColl>(TColl collection, IDatabaseMapper<T> mapper, CancellationToken cancellationToken = default) where TColl : ICollection<T>
        => SelectInternalAsync(collection, mapper, false, false, int.MaxValue, nameof(SelectQueryAsync), cancellationToken);
}