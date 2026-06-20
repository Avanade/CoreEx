namespace CoreEx.Database.Extended;

/// <summary>
/// Provides the base <see cref="IDatabase"/> multi-set arguments when expecting a collection of items/records.
/// </summary>
public abstract class MultiSetCollArgs : IMultiSetArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MultiSetCollArgs"/> class.
    /// </summary>
    /// <param name="minimumRows">The minimum number of rows allowed.</param>
    /// <param name="maximumRows">The maximum number of rows allowed.</param>
    /// <param name="stopOnNull">Indicates whether to stop further query result set processing where the current set has resulted in a <see langword="null"/> (i.e. no records).</param>
    public MultiSetCollArgs(int minimumRows = 0, int? maximumRows = null, bool stopOnNull = false)
    {
        if (maximumRows.HasValue && minimumRows <= maximumRows.Value)
            throw new ArgumentException("Max Rows is less than Min Rows.", nameof(maximumRows));

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
    public abstract void DatasetRecord(DatabaseRecord dr);

    /// <inheritdoc/>
    public virtual void InvokeResult() { }
}