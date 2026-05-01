namespace CoreEx.Database;

public abstract partial class DatabaseCommand
{
    /// <summary>
    /// Selects a single item.
    /// </summary>
    /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
    /// <param name="mapper">The <see cref="IDatabaseMapper{T}"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The single item.</returns>
    public async Task<T> SelectSingleAsync<T>(IDatabaseMapper<T> mapper, CancellationToken cancellationToken = default)
    {
        var result = await SelectSingleFirstInternalAsync(mapper, true, nameof(SelectSingleAsync), cancellationToken).ConfigureAwait(false);
        return result ?? throw new InvalidOperationException($"{nameof(SelectSingleAsync)} has not returned a row.");
    }

    /// <summary>
    /// Selects a single item or default.
    /// </summary>
    /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
    /// <param name="mapper">The <see cref="IDatabaseMapper{T}"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The single item or default.</returns>
    public async Task<T?> SelectSingleOrDefaultAsync<T>(IDatabaseMapper<T> mapper, CancellationToken cancellationToken = default)
        => await SelectSingleFirstInternalAsync(mapper, true, nameof(SelectSingleOrDefaultAsync), cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Selects first item.
    /// </summary>
    /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
    /// <param name="mapper">The <see cref="IDatabaseMapper{T}"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The single item.</returns>
    public async Task<T> SelectFirstAsync<T>(IDatabaseMapper<T> mapper, CancellationToken cancellationToken = default)
    {
        var result = await SelectSingleFirstInternalAsync(mapper, false, nameof(SelectFirstAsync), cancellationToken).ConfigureAwait(false);
        return result ?? throw new InvalidOperationException($"{nameof(SelectFirstAsync)} has not returned a row.");
    }

    /// <summary>
    /// Selects first item or default.
    /// </summary>
    /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
    /// <param name="mapper">The <see cref="IDatabaseMapper{T}"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The single item or default.</returns>
    public async Task<T?> SelectFirstOrDefaultAsync<T>(IDatabaseMapper<T> mapper, CancellationToken cancellationToken = default)
        => await SelectSingleFirstInternalAsync(mapper, false, nameof(SelectFirstOrDefaultAsync), cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Select first row result only (where exists) internal.
    /// </summary>
    private async Task<T> SelectSingleFirstInternalAsync<T>(IDatabaseMapper<T> mapper, bool throwWhereMulti, string memberName, CancellationToken cancellationToken)
    {
        var coll = new List<T>();
        await SelectInternalAsync(coll, mapper, throwWhereMulti, false, 2, memberName, cancellationToken).ConfigureAwait(false);
        return coll.Count == 0 ? default! : coll[0];
    }

    /// <summary>
    /// Select the rows from the query internal.
    /// </summary>
    private async Task SelectInternalAsync<T, TColl>(TColl coll, IDatabaseMapper<T> mapper, bool throwWhereMulti, bool stopAfterOneRow, int maxRows, string memberName, CancellationToken cancellationToken) where TColl : ICollection<T>
    {
        mapper.ThrowIfNull();

        await Database.Invoker.InvokeAsync(Database, DbArgs, async (_, _, cancellationToken) =>
        {
            int i = 0;

            using var cmd = await CreateCommandAsync(cancellationToken).ConfigureAwait(false);
            using var dr = await LogCommand(cmd).ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

            while (await dr.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                if (++i == 2)
                {
                    if (throwWhereMulti)
                        throw new InvalidOperationException($"{memberName} has returned more than one row.");

                    if (stopAfterOneRow)
                        return;
                }

                if (i - 1 >= maxRows)
                    return;

                coll.Add(mapper.MapFromDb(new DatabaseRecord(Database, dr)) ?? throw new InvalidOperationException("A null must not be returned from the mapper."));
                if (!throwWhereMulti && stopAfterOneRow)
                    return;
            }

            return;
        }, cancellationToken, memberName).ConfigureAwait(false);
    }
}