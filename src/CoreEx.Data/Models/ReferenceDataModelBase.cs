namespace CoreEx.Data.Models;

/// <summary>
/// Provides a convenience base class for reference data models implementing the common <see cref="IReferenceData"/> properties (extends <see cref="ModelBase{T}"/>).
/// </summary>
/// <typeparam name="TId">The identifier <see cref="Type"/>.</typeparam>
/// <remarks>Usage is purely optional; there is no other specific requirement for its use.
/// <para>Does not implement <see cref="IReferenceData"/> by design, as it is not intended to support the base functionality.</para></remarks>
public abstract class ReferenceDataModelBase<TId> : ModelBase<TId>
{
    /// <summary>
    /// Gets or sets the unique code.
    /// </summary>
    public string Code { get; set; } = default!;

    /// <summary>
    /// Gets or sets the text.
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// Gets or sets the description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the sort order.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Indicates whether the reference data is active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets the validity start <see cref="DateTimeOffset"/>.
    /// </summary>
    public DateTimeOffset? StartsOn { get; init; }

    /// <summary>
    /// Gets or sets the validity end <see cref="DateTimeOffset"/>.
    /// </summary>
    public DateTimeOffset? EndsOn { get; init; }
}