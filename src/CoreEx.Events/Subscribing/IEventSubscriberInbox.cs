namespace CoreEx.Events.Subscribing;

/// <summary>
/// Enables an <see cref="EventSubscriberBase"/> inbox pattern to determine whether an event/message should be processed; for example, to track and detect duplicates to enable idempotent event/message handling.
/// </summary>
public interface IEventSubscriberInbox
{
    /// <summary>
    /// Performs an inbox check on the <paramref name="event"/>/message.
    /// </summary>
    /// <param name="event">The <see cref="EventData"/>.</param>
    /// <param name="args">The <see cref="EventSubscriberArgs"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns><see langword="true"/> indicates that the event/message should be processed; otherwise, <see langword="false"/>.</returns>
    Task<bool> InboxCheckAsync(EventData @event, EventSubscriberArgs args, CancellationToken cancellationToken = default);
}