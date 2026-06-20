namespace CoreEx.Data.Models;

/// <summary>
/// Provides a convenience base class for data models implementing the common properties and interfaces such as <see cref="IIdentifier{T}"/>, <see cref="IChangeLogEx"/>, and <see cref="IETag"/>.
/// </summary>
/// <typeparam name="TId">The identifier <see cref="Type"/>.</typeparam>
/// <remarks>Usage is purely optional; there is no other specific requirement for its use.</remarks>
public abstract class ModelBase<TId> : IIdentifier<TId>, IChangeLogEx, IETag
{
    /// <summary>
    /// Gets or sets the identifier.
    /// </summary>
    public TId Id { get; set; } = default!;

    /// <inheritdoc/>
    public string? CreatedBy { get; set; }

    /// <inheritdoc/>
    public DateTimeOffset? CreatedOn { get; set; }

    /// <inheritdoc/>
    public string? UpdatedBy { get; set; }

    /// <inheritdoc/>
    public DateTimeOffset? UpdatedOn { get; set; }

    /// <inheritdoc/>
    public string? ETag { get; set; }
}