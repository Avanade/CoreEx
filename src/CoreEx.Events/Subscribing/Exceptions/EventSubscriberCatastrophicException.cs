namespace CoreEx.Events.Subscribing.Exceptions;

/// <summary>
/// Represents an exception that occurs when an <see cref="EventSubscriberBase"/> encounters a <see cref="ErrorHandling.Catastrophic"/> error while processing a message/event.
/// </summary>
/// <remarks>This exception is intended primarily for internal use only and should not be thrown directly by user receiving/subscribing code.</remarks>
/// <param name="message">The error message.</param>
/// <param name="innerException">The optional inner <see cref="Exception"/>.</param>
public sealed class EventSubscriberCatastrophicException(string? message = null, Exception? innerException = null)
    : Exception(message ?? "A catastrophic failure occurred during the event subscriber processing.", innerException), IEventSubscriberException
{
    /// <inheritdoc/>
    public ErrorHandling ErrorHandling => ErrorHandling.Catastrophic;
}