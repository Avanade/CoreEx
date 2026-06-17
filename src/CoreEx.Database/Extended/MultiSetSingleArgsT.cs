namespace CoreEx.Database.Extended;

/// <summary>
/// Provides the <see cref="IDatabase"/> multi-set arguments when expecting a single item/record only.
/// </summary>
/// <typeparam name="T">The item <see cref="Type"/>.</typeparam>
/// <param name="mapper">The <see cref="IDatabaseMapper{TItem}"/> for the <see cref="DatabaseRecord"/>.</param>
/// <param name="result">The action that will be invoked with the result of the set.</param>
/// <param name="isMandatory">Indicates whether the value is mandatory; defaults to <see langword="true"/>.</param>
/// <param name="stopOnNull">Indicates whether to stop further query result set processing where the current set has resulted in a null (i.e. no records).</param>
public class MultiSetSingleArgs<T>(IDatabaseMapper<T> mapper, Action<T> result, bool isMandatory = true, bool stopOnNull = false) : MultiSetSingleArgs(isMandatory, stopOnNull), IMultiSetArgs<T> where T : class, new()
{
    private T? _value;
    private readonly Action<T> _result = result.ThrowIfNull();

    /// <summary>
    /// Gets the <see cref="IDatabaseMapper{T}"/> for the <see cref="DatabaseRecord"/>.
    /// </summary>
    public IDatabaseMapper<T> Mapper { get; } = mapper.ThrowIfNull();

    /// <inheritdoc/>
    public override void DatasetRecord(DatabaseRecord dr) => _value = Mapper.MapFromDb(dr);

    /// <inheritdoc/>
    public override void InvokeResult()
    {
        if (_value is not null)
            _result(_value);
    }
}