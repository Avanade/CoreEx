namespace CoreEx.Entities;

/// <summary>
/// Enables a mutable <see cref="ETag"/> for the likes of versioning (optimistic concurrency).
/// </summary>
public interface IETag : IReadOnlyETag
{
    /// <inheritdoc/>
    string? IReadOnlyETag.ETag => ETag;

    /// <summary>
    /// Gets or sets the entity tag.
    /// </summary>
    new string? ETag { get; set; }
}