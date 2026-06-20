namespace CoreEx.DomainDriven;

/// <summary>
/// Enables the <see href="https://en.wikipedia.org/wiki/Domain-driven_design">domain-driven</see> aggregate functionality.
/// </summary>
/// <remarks>An <see cref="IAggregateRoot"/> is an <see cref="IEntity"/> that acts as the root for a cluster of associated objects (entities and value objects). The aggregate supports related <i>integration</i> <see cref="Events"/>;
/// however, the aggregate root does <i>not</i> support <i>domain</i> events (this is by design).</remarks>
public interface IAggregateRoot : IEntity
{
    /// <summary>
    ///    Gets a read-only collection of <see cref="EventData">events</see>.
    /// </summary>
    /// <remarks>Events are typically used for <i>integration</i> purposes to inform other systems of changes that have occurred to/within the aggregate root. These events are not to be confused with <i>domain</i> events; which are not natively supported (this is by design).</remarks>
    [JsonIgnore]
    IReadOnlyCollection<EventData> Events { get; }

    /// <summary>
    /// Indicates whether the aggregate has any events.
    /// </summary>
    [JsonIgnore]
    bool HasEvents { get; }
}