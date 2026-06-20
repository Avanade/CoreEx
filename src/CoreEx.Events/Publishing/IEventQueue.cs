namespace CoreEx.Events.Publishing;

/// <summary>
/// Defines the standardized <i>event</i> adding/queueing. 
/// </summary>
public interface IEventQueue
{
    /// <summary>
    /// Indicates whether the internal queue is empty.
    /// </summary>
    /// <returns><see langword="true"/> where empty; otherwise, <see langword="false"/>.</returns>
    bool IsEmpty { get; }

    /// <summary>
    /// Gets the internal queue count.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Adds (queues in-process) one or more <see cref="EventData"/> <paramref name="events"/> to the default destination ready for <see cref="IEventPublisher.PublishAsync"/>.
    /// </summary>
    /// <param name="events">Zero or more <see cref="EventData"/> objects to publish.</param>
    /// <remarks>A <i>destination</i> is synonymous with a <i>topic name</i> depending on the underlying messaging system.</remarks>
    void Add(params IEnumerable<EventData> @events);

    /// <summary>
    /// Adds (queues in-process) one or more <see cref="EventData"/> <paramref name="events"/> to the specified <paramref name="destination"/> ready for <see cref="IEventPublisher.PublishAsync"/>.
    /// </summary>
    /// <param name="destination">The destination name.</param>
    /// <param name="events">Zero or more <see cref="EventData"/> objects to publish.</param>
    /// <remarks>A <paramref name="destination"/> is synonymous with a <i>topic name</i> depending on the underlying messaging system.</remarks>
    void Add(string destination, params IEnumerable<EventData> @events);

    /// <summary>
    /// Adds (queues in-process) one or more <see cref="EventData"/> <paramref name="events"/> to the specified <paramref name="destination"/> ready for <see cref="IEventPublisher.PublishAsync"/>.
    /// </summary>
    /// <param name="destination">The destination name.</param>
    /// <param name="events">Zero or more <see cref="CloudEvent"/> objects to publish.</param>
    /// <remarks>A <paramref name="destination"/> is synonymous with a <i>topic name</i> depending on the underlying messaging system.</remarks>
    void Add(string destination, params IEnumerable<CloudEvent> @events);

    /// <summary>
    /// Adds (queues in-process) one or more <see cref="DestinationEvent"/> <paramref name="events"/> ready for <see cref="IEventPublisher.PublishAsync"/>.
    /// </summary>
    /// <param name="events">Zero or more <see cref="DestinationEvent"/> objects to publish.</param>
    void Add(params IEnumerable<DestinationEvent> @events);

    /// <summary>
    /// Clears the internal queue of all previously added (queued) events without publishing them.
    /// </summary>
    void Clear();
}