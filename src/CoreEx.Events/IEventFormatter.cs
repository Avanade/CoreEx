namespace CoreEx.Events;

/// <summary>
/// Enables the formatting (<see cref="Format"/>) and parsing (<see cref="Parse"/>) of an <see cref="EventData"/>, and its conversion to (<see cref="ConvertToCloudEvent"/>) and from (<see cref="ConvertFromCloudEvent"/>) a <see cref="CloudEvent"/>.
/// </summary>
public interface IEventFormatter
{
    /// <summary>
    /// Formats the <paramref name="event"/>; this should be performed before the <see cref="ConvertToCloudEvent(EventData)"/>.
    /// </summary>
    /// <param name="event">The <see cref="EventData"/>.</param>
    /// <returns>The <see cref="EventData"/> to support fluent-style method-chaining.</returns>
    EventData Format(EventData @event);

    /// <summary>
    /// Parses the <paramref name="event"/>; this should be performed after the <see cref="ConvertFromCloudEvent(CloudEvent)"/>.
    /// </summary>
    /// <param name="event">The <see cref="EventData"/>.</param>
    /// <returns>The <see cref="EventData"/> to support fluent-style method-chaining.</returns>
    EventData Parse(EventData @event);

    /// <summary>
    /// Adds tracing to the <see cref="CloudEvent"/>.
    /// </summary>
    /// <param name="cloudEvent">The <see cref="CloudEvent"/>.</param>
    /// <param name="traceParent">The optional trace parent; defaults to <see cref="Activity.TraceId"/>.</param>
    /// <param name="traceState">The optional trace state; defaults to <see cref="Activity.TraceStateString"/>.</param>
    /// <param name="traceBaggage">The optional trace baggage; defaults to <see cref="Activity.Baggage"/>.</param>
    /// <remarks>To add, as a minimum the <paramref name="traceParent"/> must be specified.</remarks>
    void AddTracing(CloudEvent cloudEvent, string? traceParent = null, string? traceState = null, IEnumerable<KeyValuePair<string, string?>>? traceBaggage = null);

    /// <summary>
    /// Converts an <see cref="EventData"/> <i>to</i> a <see cref="CloudEvent"/>.
    /// </summary>
    /// <param name="event">The <see cref="EventData"/>.</param>
    /// <returns>The resulting <see cref="CloudEvent"/>.</returns>
    CloudEvent ConvertToCloudEvent(EventData @event);

    /// <summary>
    /// Converts <i>from</i> a <see cref="CloudEvent"/> to an <see cref="EventData"/>.
    /// </summary>
    /// <param name="cloudEvent">The <see cref="CloudEvent"/>.</param>
    /// <returns>The resulting <paramref name="cloudEvent"/>.</returns>
    EventData ConvertFromCloudEvent(CloudEvent cloudEvent);
}