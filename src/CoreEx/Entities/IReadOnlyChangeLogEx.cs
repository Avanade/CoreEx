namespace CoreEx.Entities;

/// <summary>
/// Enables a read-only <see cref="CreatedBy"/>, <see cref="CreatedOn"/>, <see cref="UpdatedBy"/>, and <see cref="UpdatedOn"/>.
/// </summary>
public interface IReadOnlyChangeLogEx
{
    /// <summary>
    /// Gets the user who created the entity.
    /// </summary>
    string? CreatedBy { get; }

    /// <summary>
    /// Gets the timestamp of when the entity was created.
    /// </summary>
    DateTimeOffset? CreatedOn { get; }

    /// <summary>
    /// Gets the user who last updated the entity.
    /// </summary>
    string? UpdatedBy { get; }

    /// <summary>
    /// Gets the timestamp of when the entity was last updated.
    /// </summary>
    DateTimeOffset? UpdatedOn { get; }
}