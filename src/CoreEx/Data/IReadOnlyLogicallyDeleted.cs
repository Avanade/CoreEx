namespace CoreEx.Data;

/// <summary>
/// Enables a read-only logical <see cref="IsDeleted"/> data model state.
/// </summary>
public interface IReadOnlyLogicallyDeleted
{
    /// <summary>
    /// Indicates whether the data model is considered logically deleted.
    /// </summary>
    bool IsDeleted { get; }
}