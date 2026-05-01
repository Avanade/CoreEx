namespace CoreEx.DomainDriven;

/// <summary>
/// Provides the typed <see href="https://en.wikipedia.org/wiki/Domain-driven_design">domain-driven</see> aggregate functionality.
/// </summary>
/// <typeparam name="TId">The identifier <see cref="Type"/>.</typeparam>
/// <typeparam name="TSelf">The <see cref="Aggregate{TId, TSelf}"/> <see cref="Type"/> itself.</typeparam>
/// <param name="id">The identifier.</param>
/// <remarks>An aggregate is ostensibly an entity with additional <see cref="Events"/> support as enabled by the <see cref="IAggregateRoot"/>. The events are typically used for <i>integration</i> purposes to inform other systems of 
/// changes that have occurred to/within the aggregate root. The events are not to be confused with <i>domain</i> events; which are not natively supported (this is by design).
/// <para>The <see cref="Events"/> collection is a temporary storage for events that have been raised during the lifetime of the aggregate; these would then need to be forwarded (by the implementor) to the appropriate <see cref="Events.Publishing.IEventQueue">event handlers</see> for processing.</para> 
/// <para>It is expected that the implementor will adhere to the principles of domain-driven design and only expose read-only properties, and enable modification through methods, ensuring the invariant nature of the aggregate.</para></remarks>
public class Aggregate<TId, TSelf>(TId id) : Entity<TId, TSelf>(id), IAggregateRoot where TSelf : Aggregate<TId, TSelf>
{
    private readonly ICollection<EventData> _events = [];

    /// <inheritdoc/>
    [JsonIgnore]
    public IReadOnlyCollection<EventData> Events => (IReadOnlyCollection<EventData>)_events;

    /// <inheritdoc/>
    [JsonIgnore]
    public bool HasEvents => _events.Count > 0;

    /// <summary>
    /// Adds the specified <see cref="EventData"/> to the aggregate.
    /// </summary>
    /// <param name="eventData">The <see cref="EventData"/>.</param>
    protected TSelf AddEvent(EventData eventData)
    {
        _events.Add(eventData);
        return (TSelf)this;
    }

    /// <summary>
    /// Clears all events from the aggregate.
    /// </summary>
    protected TSelf ClearEvents()
    {
        _events.Clear();
        return (TSelf)this;
    }
}