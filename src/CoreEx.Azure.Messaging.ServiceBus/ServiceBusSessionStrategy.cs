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
    /// <remarks>The <see cref="ServiceBusMessage.PartitionKey"/> is generally set from the corresponding <see cref="EventData.PartitionKey"/>.
    /// <para>Where an event has no <see cref="EventData.PartitionKey"/> (a <see cref="ServiceBusMessage.SessionId"/> is still required by a session-enabled entity), the publisher falls back to a single fixed
    /// value (see <see cref="ServiceBusPublisher.NoPartitionKeySessionId"/>) rather than a new value per message - this bounds the number of sessions created for partition-key-less events to at most one extra
    /// session (rather than growing unbounded over time) and preserves their relative publish order. If partition-key-less events are expected to represent a significant proportion of overall throughput, they
    /// will all serialize through that one session; in that case those events should generally be given a real partition key instead.</para></remarks>
    UsePartitionKeyAsIs,

    /// <summary>
    /// Uses the <see cref="ServiceBusMessage.PartitionKey"/> converted to a <see cref="ServiceBusMessage.SessionId"/> using <see cref="Data.PartitionKey.GetPartitionId"/>.
    /// </summary>
    /// <remarks>Where the underlying partition-key value is such that there may be 100s/1000s/10000s+ of possible values, then leveraging this strategy with a sensible partition-size will help to ensure that the number of sessions is kept to a manageable level; e.g. 8, 16, 32, 64, etc.
    /// This will aid the receiver-side where sessions are used to ensure that concurrent processing is spread across a smaller number of sessions (and thus more efficient) rather than having a large number of sessions with only a few messages in each. However,
    /// note that there should be at least as many session receivers as the number of sessions to ensure that all sessions are being processed concurrently; in a fair and equable rate - this will avoid "hot" and "cold" sessions where some sessions are receiving more messages than
    /// others and thus processing is not spread across the session receivers as well as it could be.
    /// <para>The <see cref="ServiceBusMessage.PartitionKey"/> is generally set from the corresponding <see cref="EventData.PartitionKey"/>.</para>
    /// <para>Where an event has no <see cref="EventData.PartitionKey"/>, the same fixed value used by <see cref="UsePartitionKeyAsIs"/> (see <see cref="ServiceBusPublisher.NoPartitionKeySessionId"/>) is used as
    /// input to the conversion, so all such events consistently land in the same one of the existing pool of sessions (rather than growing the pool, or being spread randomly across it) - this preserves their
    /// relative publish order, at the cost of concentrating them into that one bucket alongside whatever partition-key values also happen to hash there.</para></remarks>
    UsePartitionKeyConvertedToAnId
}