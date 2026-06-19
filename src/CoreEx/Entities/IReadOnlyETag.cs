namespace CoreEx.Entities;

/// <summary>
/// Enables a read-only <see cref="ETag"/> for the likes of versioning (optimistic concurrency).
/// </summary>
public interface IReadOnlyETag
{
    /// <summary>
    /// Gets the entity tag.
    /// </summary>
    string? ETag { get; }
}