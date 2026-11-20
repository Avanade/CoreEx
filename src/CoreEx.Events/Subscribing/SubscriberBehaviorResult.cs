namespace CoreEx.Events.Subscribing;

/// <summary>
/// Provides <see cref="ISubscriberBehavior"/> short-circuit results.
/// </summary>
/// <remarks>Each result is classified and logged by the pipeline using the owning subscriber's <see cref="EventSubscriberBase.ErrorHandler"/> rules for the stated <see cref="ErrorHandling"/>.</remarks>
public static class SubscriberBehaviorResult
{
    /// <summary>
    /// Skips the subscriber and completes the message.
    /// </summary>
    /// <param name="reason">The reason.</param>
    /// <param name="completion">The completion <see cref="ErrorHandling"/>; must be one of the <c>CompleteAs*</c> values. Defaults to <see cref="ErrorHandling.CompleteAsSilent"/>.</param>
    /// <returns>The <see cref="Result"/>.</returns>
    public static Result Skip(string reason, ErrorHandling completion = ErrorHandling.CompleteAsSilent)
        => completion is ErrorHandling.CompleteAsSilent or ErrorHandling.CompleteAsInformation or ErrorHandling.CompleteAsWarning or ErrorHandling.CompleteAsError
            ? new EventSubscriberReceiveException(reason, completion)
            : throw new ArgumentOutOfRangeException(nameof(completion), completion, "Only CompleteAs* error handling is supported for a skip.");

    /// <summary>
    /// Requests that the delivery be retried.
    /// </summary>
    /// <param name="reason">The reason.</param>
    /// <returns>The <see cref="Result"/>.</returns>
    public static Result Retry(string reason) => new EventSubscriberReceiveException(reason, ErrorHandling.Retry);

    /// <summary>
    /// Requests that the message be dead-lettered.
    /// </summary>
    /// <param name="reason">The broker-facing dead-letter reason.</param>
    /// <returns>The <see cref="Result"/>.</returns>
    public static Result DeadLetter(string reason) => new EventSubscriberReceiveException(reason, ErrorHandling.DeadLetter);
}
