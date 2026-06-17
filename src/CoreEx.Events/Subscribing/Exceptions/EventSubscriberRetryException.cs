namespace CoreEx.Events.Subscribing.Exceptions;

/// <summary>
/// Represents an exception that occurs when an <see cref="EventSubscriberBase"/> needs to perform a <see cref="ErrorHandling.Retry"/>.
/// </summary>
/// <param name="message">The error message.</param>
/// <param name="innerException">The optional inner <see cref="Exception"/>.</param>
public sealed class EventSubscriberRetryException(string? message = null, Exception? innerException = null)
    : Exception(message ?? "An error occurred in the event subscriber that is considered transient and is a candidate for a retry.", innerException), IEventSubscriberException
{
    /// <inheritdoc/>
    public ErrorHandling ErrorHandling => ErrorHandling.Retry;
}