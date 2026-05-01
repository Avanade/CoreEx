namespace CoreEx.Events.Publishing;

/// <summary>
/// Enables a standardized means to create/provide the destination (i.e. topic) name.
/// </summary>
/// <remarks>The dead-letter capabilities are only leveraged where this is not a native capability of the underlying messaging system (i.e. Kafka).</remarks>
public interface IDestinationProvider
{
    /// <summary>
    /// Creates the destination name from an <paramref name="event"/>.
    /// </summary>
    /// <param name="event">The <see cref="EventData"/>.</param>
    /// <param name="isDeadLetter">Indicates whether to provide a dead-letter specific destination or not.</param>
    /// <returns>The resulting destination name.</returns>
    string CreateFrom(EventData @event, bool isDeadLetter = false);

    /// <summary>
    /// Creates the destination name from an existing <paramref name="destination"/>.  
    /// </summary>
    /// <param name="destination">The existing destination name.</param>
    /// <param name="isDeadLetter">Indicates whether to provide a dead-letter specific destination or not.</param>
    /// <returns>The resulting destination name.</returns>
    string CreateFrom(string destination, bool isDeadLetter = false);

    /// <summary>
    /// Creates the destination name using the specified parameters.
    /// </summary>
    /// <param name="domainName">The recipient domain (DDD) name.</param>
    /// <param name="messageType">The <see cref="MessageType"/>.</param>
    /// <param name="isDeadLetter">Indicates whether to provide a dead-letter specific destination or not.</param>
    /// <returns>The resulting destination name.</returns>
    string CreateNew(MessageType messageType = MessageType.Event, string? domainName = null, bool isDeadLetter = false);
}