namespace CoreEx.Events.Subscribing.Exceptions;

/// <summary>
/// Represents an exception that occurs when a message/event error occurs during a subscriber receive with an <paramref name="errorHandling"/> override.
/// </summary>
/// <param name="message">The message.</param>
/// <param name="errorHandling">The <see cref="ErrorHandling"/>.</param>
/// <param name="innerException">The optional inner <see cref="Exception"/>.</param>
/// <remarks>This is an internal exception leveraged to manage an error during subscriber receive processing; this should eventually be converted to an external facing <see cref="IEventSubscriberException"/> type.</remarks>
internal sealed class EventSubscriberReceiveException(string message, ErrorHandling errorHandling, Exception? innerException = null) : Exception(message.ThrowIfNullOrEmpty(), innerException)
{
    /// <summary>
    /// Gets the <see cref="Subscribing.ErrorHandling"/>.
    /// </summary>
    public ErrorHandling ErrorHandling { get; } = errorHandling;
}