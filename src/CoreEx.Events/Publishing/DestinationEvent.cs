namespace CoreEx.Events.Publishing;

/// <summary>
/// Provides the <see cref="Destination"/> and <see cref="Event"/> pairing for the <see cref="EventPublisherBase.OnPublishAsync(CoreEx.Events.Publishing.DestinationEvent[], CancellationToken)"/>.
/// </summary>
public sealed record class DestinationEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DestinationEvent"/> class.
    /// </summary>
    /// <param name="destination">The destination (i.e. topic) name.</param>
    /// <param name="event">The <see cref="CloudNative.CloudEvents.CloudEvent"/>.</param>
    public DestinationEvent(string destination, CloudEvent @event)
    {
        Destination = destination.ThrowIfNullOrEmpty();
        Event = @event.ThrowIfNull();
    }

    /// <summary>
    /// Gets the destination (i.e. topic) name.
    /// </summary>
    public string Destination { get; }

    /// <summary>
    /// Gets the <see cref="CloudNative.CloudEvents.CloudEvent"/>.
    /// </summary>
    public CloudEvent Event { get; }
}