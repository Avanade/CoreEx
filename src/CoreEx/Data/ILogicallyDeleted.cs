namespace CoreEx.Data;

/// <summary>
/// Enables a mutable logical <see cref="IsDeleted"/> data model state.
/// </summary>
public interface ILogicallyDeleted : IReadOnlyLogicallyDeleted
{
    /// <inheritdoc/>
    bool IReadOnlyLogicallyDeleted.IsDeleted => IsDeleted;

    /// <summary>
    /// Indicates whether the data model is considered logically deleted.
    /// </summary>
    new bool IsDeleted { get; set; }
}