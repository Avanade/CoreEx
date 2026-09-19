namespace CoreEx.Database.Extended;

/// <summary>
/// Enables the <see cref="IDatabase"/> multi-set arguments
/// </summary>
public interface IMultiSetArgs : IMultiSetArgsCore
{
    /// <summary>
    /// The <see cref="DatabaseRecord"/> method invoked for each record for its respective dataset.
    /// </summary>
    /// <param name="dr">The <see cref="DatabaseRecord"/>.</param>
    void DatasetRecord(DatabaseRecord dr);
}