namespace CoreEx.Database.Extended;

/// <summary>
/// Provides the <see cref="IDatabase"/> multi-set arguments when expecting a collection of items/records.
/// </summary>
/// <typeparam name="TColl">The collection <see cref="Type"/>.</typeparam>
/// <typeparam name="TItem">The item <see cref="Type"/>.</typeparam>
/// <param name="mapper">The <see cref="IDatabaseMapper{TItem}"/> for the <see cref="DatabaseRecord"/>.</param>
/// <param name="result">The action that will be invoked with the result of the set.</param>
/// <param name="minimumRows">The minimum number of rows allowed.</param>
/// <param name="maximumRows">The maximum number of rows allowed.</param>
/// <param name="stopOnNull">Indicates whether to stop further query result set processing where the current set has resulted in a <see langword="null"/> (i.e. no records).</param>
public class MultiSetCollArgs<TColl, TItem>(IDatabaseMapper<TItem> mapper, Action<TColl> result, int minimumRows = 0, int? maximumRows = null, bool stopOnNull = false) : MultiSetCollArgs(minimumRows, maximumRows, stopOnNull), IMultiSetArgs<TItem>
    where TItem : class, new()
    where TColl : class, ICollection<TItem>, new()
{
    private TColl? _coll;
    private readonly Action<TColl> _result = result.ThrowIfNull();

    /// <summary>
    /// Gets the <see cref="IDatabaseMapper{TSource}"/> for the <see cref="DatabaseRecord"/>.
    /// </summary>
    public IDatabaseMapper<TItem> Mapper { get; } = mapper.ThrowIfNull();

    /// <inheritdoc/>
    public override void DatasetRecord(DatabaseRecord dr)
    {
        dr.ThrowIfNull();
        _coll ??= new TColl();

        var item = Mapper.MapFromDb(dr);
        if (item is not null)
            _coll.Add(item);
    }

    /// <inheritdoc/>
    public override void InvokeResult()
    {
        if (_coll is not null)
            _result(_coll);
    }
}