namespace CoreEx.Events.Subscribing.Exceptions;

/// <summary>
/// Represents an exception that occurs when a message/event is unable to be processed and should be either flagged as or forwarded as a <see cref="ErrorHandling.DeadLetter"/>.
/// </summary>
/// <remarks>This exception is intended primarily for internal use only and should not be thrown directly by user receiving/subscribing code.</remarks>
/// <param name="message">The optional message.</param>
/// <param name="innerException">The optional inner <see cref="Exception"/>.</param>
public sealed class EventSubscriberDeadLetterException(string? message = null, Exception? innerException = null)
    : Exception(message ?? "An error occurred that is unable to be processed and has been flagged for dead-letter processing.", innerException), IEventSubscriberException
{
    /// <inheritdoc/>
    public ErrorHandling ErrorHandling => ErrorHandling.DeadLetter;
}