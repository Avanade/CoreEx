namespace CoreEx.Events.Subscribing;

/// <summary>
/// Describes the final, once-per-delivery outcome observed by <see cref="ISubscriberBehavior.OnDispositionAsync"/>.
/// </summary>
/// <remarks>Separates <i>what</i> was applied (<see cref="Settlement"/>) from <i>why</i> (<see cref="Classification"/>); for example, a <see cref="ErrorHandling.Catastrophic"/> classification is abandoned by a
/// Service Bus receiver but left unsettled by an Azure Functions host.</remarks>
public sealed class SubscriberDisposition
{
    /// <summary>
    /// Gets the settlement actually applied.
    /// </summary>
    public required SubscriberSettlement Settlement { get; init; }

    /// <summary>
    /// Gets the settlement that was attempted where <see cref="Settlement"/> is <see cref="SubscriberSettlement.SettlementFailed"/>.
    /// </summary>
    public SubscriberSettlement? AttemptedSettlement { get; init; }

    /// <summary>
    /// Gets the final processing classification; <see langword="null"/> indicates success (or cancellation).
    /// </summary>
    public ErrorHandling? Classification { get; init; }

    /// <summary>
    /// Gets the final processing error, where applicable.
    /// </summary>
    public Exception? Error { get; init; }

    /// <summary>
    /// Gets the settlement error where <see cref="Settlement"/> is <see cref="SubscriberSettlement.SettlementFailed"/>.
    /// </summary>
    public Exception? SettlementError { get; init; }

    /// <summary>
    /// Indicates whether the settlement failed because the message or session lock was lost.
    /// </summary>
    public bool IsLockLost { get; init; }

    /// <summary>
    /// Indicates whether processing ended because the receive cancellation token was cancelled (e.g. host shutdown).
    /// </summary>
    public bool IsCancelled { get; init; }

    /// <summary>
    /// Indicates whether the receiver was requested to pause as a result of a <see cref="ErrorHandling.Catastrophic"/> classification.
    /// </summary>
    /// <remarks>Reflects the pause decision, not completion of the pause itself.</remarks>
    public bool ReceiverPaused { get; init; }
}
