namespace CoreEx.Database.Extended;

/// <summary>
/// Provides the base <see cref="IDatabase"/> multi-set arguments when expecting a single item/record only.
/// </summary>
/// <param name="isMandatory">Indicates whether the value is mandatory; defaults to <see langword="true"/>.</param>
/// <param name="stopOnNull">Indicates whether to stop further query result set processing where the current set has resulted in a null (i.e. no records).</param>
public abstract class MultiSetSingleArgs(bool isMandatory = true, bool stopOnNull = false) : IMultiSetArgs
{
    /// <summary>
    /// Indicates whether the value is mandatory; i.e. a corresponding record must be read.
    /// </summary>
    public bool IsMandatory { get; set; } = isMandatory;

    /// <inheritdoc/>
    public int MinimumRows => IsMandatory ? 1 : 0;

    /// <inheritdoc/>
    public int? MaximumRows => 1;

    /// <inheritdoc/>
    public bool StopOnNull { get; set; } = stopOnNull;

    /// <inheritdoc/>
    public abstract void DatasetRecord(DatabaseRecord dr);

    /// <inheritdoc/>
    public virtual void InvokeResult() { }
}
