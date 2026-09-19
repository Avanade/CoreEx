namespace CoreEx.Cosmos.Extended;

/// <summary>
/// Provides the <see cref="ICosmosDb"/> multi-set arguments when expecting a single item only.
/// </summary>
/// <typeparam name="TModel">The model <see cref="Type"/>.</typeparam>
/// <param name="result">The action that will be invoked with the result.</param>
/// <param name="isMandatory">Indicates whether the value is mandatory; defaults to <see langword="true"/>.</param>
/// <param name="stopOnNull">Indicates whether to stop further multi-set result processing where the current result has resulted in a <see langword="null"/> (i.e. no matching item).</param>
public class MultiSetSingleArgs<TModel>(Action<TModel> result, bool isMandatory = true, bool stopOnNull = false) : IMultiSetArgs<TModel> where TModel : class, IEntityKey, new()
{
    private readonly Action<TModel> _result = result.ThrowIfNull();
    private TModel? _value;

    static MultiSetSingleArgs() => TypeDiscriminatorGuard.Check<TModel>();

    /// <summary>
    /// Indicates whether the value is mandatory; i.e. a corresponding item must be read.
    /// </summary>
    public bool IsMandatory { get; set; } = isMandatory;

    /// <inheritdoc/>
    public int MinimumRows => IsMandatory ? 1 : 0;

    /// <inheritdoc/>
    public int? MaximumRows => 1;

    /// <inheritdoc/>
    public bool StopOnNull { get; set; } = stopOnNull;

    /// <inheritdoc/>
    public void AddItem(TModel model) => _value = model.ThrowIfNull();

    /// <inheritdoc/>
    public void InvokeResult()
    {
        if (_value is not null)
            _result(_value);
    }
}
