namespace CoreEx.DomainDriven;

/// <summary>
/// Enables the core <see href="https://en.wikipedia.org/wiki/Domain-driven_design">domain-driven</see> entity functionality.
/// </summary>
public interface IEntity : IReadOnlyIdentifier, IReadOnlyChangeLog, IReadOnlyETag
{
    /// <summary>
    /// Gets the internal persistence state of the entity.
    /// </summary>
    [JsonIgnore]
    PersistenceState PersistenceState { get; }

    /// <summary>
    /// Indicates whether the entity is read-only.
    /// </summary>
    [JsonIgnore]
    bool IsReadOnly { get; }
}