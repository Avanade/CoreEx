namespace CoreEx.Events.Subscribing.Exceptions;

/// <summary>
/// Represents an exception that occurs when an <see cref="EventSubscriberBase"/> encounters an error that it is unable to handle bubbling it up to the host process accordingly.
/// </summary>
/// <param name="message">The error message.</param>
/// <param name="innerException">The optional inner <see cref="Exception"/>.</param>
/// <remarks>This exception is intended primarily for internal use only and should not be thrown directly by user receiving/subscribing code.</remarks>
public sealed class EventSubscriberUnhandledException(string? message = null, Exception? innerException = null)
    : Exception(message ?? "An unhandled exception occurred during the event subscriber processing.", innerException), IEventSubscriberException
{
    /// <inheritdoc/>
    public ErrorHandling ErrorHandling => ErrorHandling.None;
}