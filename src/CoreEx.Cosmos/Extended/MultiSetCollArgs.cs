namespace CoreEx.Cosmos.Extended;

/// <summary>
/// Provides the <see cref="ICosmosDb"/> multi-set arguments when expecting a collection of items.
/// </summary>
/// <typeparam name="TColl">The collection <see cref="Type"/>.</typeparam>
/// <typeparam name="TModel">The model <see cref="Type"/>.</typeparam>
public class MultiSetCollArgs<TColl, TModel> : IMultiSetArgs<TModel>
    where TModel : class, IEntityKey, new()
    where TColl : class, ICollection<TModel>, new()
{
    private readonly Action<TColl> _result;
    private TColl? _coll;

    static MultiSetCollArgs() => TypeDiscriminatorGuard.Check<TModel>();

    /// <summary>
    /// Initializes a new instance of the <see cref="MultiSetCollArgs{TColl, TModel}"/> class.
    /// </summary>
    /// <param name="result">The action that will be invoked with the result.</param>
    /// <param name="minimumRows">The minimum number of rows allowed.</param>
    /// <param name="maximumRows">The maximum number of rows allowed.</param>
    /// <param name="stopOnNull">Indicates whether to stop further multi-set result processing where the current result has resulted in a <see langword="null"/> (i.e. no matching items).</param>
    public MultiSetCollArgs(Action<TColl> result, int minimumRows = 0, int? maximumRows = null, bool stopOnNull = false)
    {
        if (maximumRows.HasValue && minimumRows > maximumRows.Value)
            throw new ArgumentException("Min Rows is greater than Max Rows.", nameof(maximumRows));

        _result = result.ThrowIfNull();
        MinimumRows = minimumRows;
        MaximumRows = maximumRows;
        StopOnNull = stopOnNull;
    }

    /// <inheritdoc/>
    public int MinimumRows { get; }

    /// <inheritdoc/>
    public int? MaximumRows { get; }

    /// <inheritdoc/>
    public bool StopOnNull { get; set; }

    /// <inheritdoc/>
    public void AddItem(TModel model)
    {
        model.ThrowIfNull();
        (_coll ??= new TColl()).Add(model);
    }

    /// <inheritdoc/>
    public void InvokeResult()
    {
        if (_coll is not null)
            _result(_coll);
    }
}
