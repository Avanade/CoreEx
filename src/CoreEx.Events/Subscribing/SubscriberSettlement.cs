namespace CoreEx.Events.Subscribing;

/// <summary>
/// Represents the message settlement actually applied by the receiving host.
/// </summary>
public enum SubscriberSettlement
{
    /// <summary>
    /// The message was completed.
    /// </summary>
    Completed,

    /// <summary>
    /// The message was abandoned and will be redelivered.
    /// </summary>
    Abandoned,

    /// <summary>
    /// The message was dead-lettered.
    /// </summary>
    DeadLettered,

    /// <summary>
    /// No settlement was attempted; the message lock will expire (or the host will settle) and the message will be redelivered.
    /// </summary>
    NotSettled,

    /// <summary>
    /// A settlement was attempted and failed; see <see cref="SubscriberDisposition.AttemptedSettlement"/> and <see cref="SubscriberDisposition.SettlementError"/>.
    /// </summary>
    SettlementFailed
}
