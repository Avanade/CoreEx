namespace CoreEx.Azure.Messaging.ServiceBus;

/// <summary>
/// Provides the <see cref="ServiceBusMessage.SessionId"/> publishing strategy.
/// </summary>
/// <remarks>See <see href="https://learn.microsoft.com/en-us/azure/service-bus-messaging/message-sessions"/> for more information on sessions.</remarks>
public enum ServiceBusSessionStrategy
{
    /// <summary>
    /// No <see cref="ServiceBusMessage.SessionId"/> is required; i.e. messages are not session-enabled.
    /// </summary>
    None,

    /// <summary>
    /// Uses the <see cref="ServiceBusMessage.PartitionKey"/> as-is (unchanged) as the <see cref="ServiceBusMessage.SessionId"/>.
    /// </summary>
    /// <remarks>The <see cref="ServiceBusMessage.PartitionKey"/> is generally set from the corresponding <see cref="EventData.PartitionKey"/>.</remarks>
    UsePartitionKeyAsIs,

    /// <summary>
    /// Uses the <see cref="ServiceBusMessage.PartitionKey"/> converted to a <see cref="ServiceBusMessage.SessionId"/> using <see cref="Data.PartitionKey.GetPartitionId"/>.
    /// </summary>
    /// <remarks>Where the underlying partition-key value is such that there may be 100s/1000s/10000s+ of possible values, then leveraging this strategy with a sensible partition-size will help to ensure that the number of sessions is kept to a manageable level; e.g. 8, 16, 32, 64, etc.
    /// This will aid the receiver-side where sessions are used to ensure that concurrent processing is spread across a smaller number of sessions (and thus more efficient) rather than having a large number of sessions with only a few messages in each. However,
    /// note that there should be at least as many session receivers as the number of sessions to ensure that all sessions are being processed concurrently; in a fair and equable rate - this will avoid "hot" and "cold" sessions where some sessions are receiving more messages than
    /// others and thus processing is not spread across the session receivers as well as it could be.
    /// <para>The <see cref="ServiceBusMessage.PartitionKey"/> is generally set from the corresponding <see cref="EventData.PartitionKey"/>.</para></remarks>
    UsePartitionKeyConvertedToAnId
}