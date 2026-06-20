namespace CoreEx.DomainDriven;

/// <summary>
/// Represents the internal persistence state of an entity.
/// </summary>
public enum PersistenceState
{
    /// <summary>
    /// Unknown, i.e. not determined.
    /// </summary>
    Unknown,

    /// <summary>
    /// New entity, i.e. not yet persisted.
    /// </summary>
    New,

    /// <summary>
    /// Not modified, i.e. unchanged since last persisted.
    /// </summary>
    NotModified,

    /// <summary>
    /// Modified, i.e. changed since last persisted.
    /// </summary>
    Modified,

    /// <summary>
    /// Removed, i.e. marked for deletion; will be deleted upon next persistence.
    /// </summary>
    Removed
}