namespace CoreEx.Events.Subscribing.Exceptions;

/// <summary>
/// Represents an exception that occurs when an <see cref="EventSubscriberBase"/> encounters an error that it is handled successfully.
/// </summary>
/// <remarks>This is an error which has handling that informs the receive as successful, appropriate logging will have occurred. See <see cref="ErrorHandling.CompleteAsSilent"/>, <see cref="ErrorHandling.CompleteAsInformation"/>,
/// <see cref="ErrorHandling.CompleteAsWarning"/> and  <see cref="ErrorHandling.CompleteAsError"/>.
/// <para>This exception is intended primarily for internal use only and should not be thrown directly by user receiving/subscribing code.</para></remarks>
/// <param name="errorHandling">The <see cref="ErrorHandling"/>.</param>
/// <param name="message">The error message.</param>
/// <param name="innerException">The optional inner <see cref="Exception"/>.</param>
public sealed class EventSubscriberHandledException(ErrorHandling errorHandling, string? message = null, Exception? innerException = null)
    : Exception(message ?? "An exception occurred during the event subscriber processing and was successfully handled.", innerException), IEventSubscriberException
{
    /// <inheritdoc/>
    public ErrorHandling ErrorHandling { get; } = errorHandling;
}