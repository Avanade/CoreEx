namespace CoreEx.Entities;

/// <summary>
/// Enables a mutable <see cref="CreatedBy"/>, <see cref="CreatedOn"/>, <see cref="UpdatedBy"/>, and <see cref="UpdatedOn"/>.
/// </summary>
public interface IChangeLogEx : IReadOnlyChangeLogEx
{
    /// <inheritdoc/>
    string? IReadOnlyChangeLogEx.CreatedBy => CreatedBy;

    /// <inheritdoc/>
    DateTimeOffset? IReadOnlyChangeLogEx.CreatedOn => CreatedOn;

    /// <inheritdoc/>
    string? IReadOnlyChangeLogEx.UpdatedBy => UpdatedBy;

    /// <inheritdoc/>
    DateTimeOffset? IReadOnlyChangeLogEx.UpdatedOn => UpdatedOn;

    /// <summary>
    /// Gets or sets the user who created the entity.
    /// </summary>
    new string? CreatedBy { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of when the entity was created.
    /// </summary>
    new DateTimeOffset? CreatedOn { get; set; }

    /// <summary>
    /// Gets or sets the user who last updated the entity.
    /// </summary>
    new string? UpdatedBy { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of when the entity was last updated.
    /// </summary>
    new DateTimeOffset? UpdatedOn { get; set; }
}