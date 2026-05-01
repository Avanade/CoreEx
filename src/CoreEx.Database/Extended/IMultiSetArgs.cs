namespace CoreEx.Database.Extended;

/// <summary>
/// Enables the <see cref="IDatabase"/> multi-set arguments
/// </summary>
public interface IMultiSetArgs
{
    /// <summary>
    /// Gets the minimum number of rows allowed.
    /// </summary>
    int MinimumRows { get; }

    /// <summary>
    /// Gets the maximum number of rows allowed.
    /// </summary>
    int? MaximumRows { get; }

    /// <summary>
    /// Indicates whether to stop further query result set processing where the current set has resulted in a <see langword="null"/> (i.e. no records).
    /// </summary>
    bool StopOnNull { get; }

    /// <summary>
    /// The <see cref="DatabaseRecord"/> method invoked for each record for its respective dataset.
    /// </summary>
    /// <param name="dr">The <see cref="DatabaseRecord"/>.</param>
    void DatasetRecord(DatabaseRecord dr);

    /// <summary>
    /// Invokes the corresponding result function.
    /// </summary>
    void InvokeResult();
}