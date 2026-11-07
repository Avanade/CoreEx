namespace CoreEx.Events;

/// <summary>
/// Defines the external representation independently of the message's event or command intent.
/// </summary>
public enum MessageRepresentation
{
    /// <summary>
    /// Publishes a CloudEvent using the transport's configured content mode.
    /// </summary>
    CloudEvent = 0,

    /// <summary>
    /// Publishes only the payload and explicitly supported native metadata.
    /// </summary>
    Native = 1
}
