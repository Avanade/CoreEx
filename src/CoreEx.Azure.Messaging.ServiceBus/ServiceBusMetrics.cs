namespace CoreEx.Azure.Messaging.ServiceBus;

/// <summary>
/// Defines the <see href="https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-messaging-overview">Azure Service Bus</see> metrics.
/// </summary>
public static class ServiceBusMetrics
{
    /// <summary>
    /// Gets the source tag name used for all Azure Service Bus metrics; represents the entity path that represents the source of the message; e.g. topic or queue name.
    /// </summary>
    public const string SourceTagName = "source";
    
    /// <summary>
    /// Gets the destination tag name used for all Azure Service Bus metrics; represents the entity path that represents the destination of the message; e.g. topic or queue name.
    /// </summary>
    public const string DestinationTagName = "destination";

    /// <summary>
    /// Gets the <see cref="Meter"/> used for recording metrics related to Azure Service Bus operations.
    /// </summary>
    public static Meter Meter { get; } = new("CoreEx.Azure.Messaging.ServiceBus");

    /// <summary>
    /// Gets the counter that tracks the number of Azure Service Bus messages sent successfully.
    /// </summary>
    public static Counter<long> MessagesSendSent { get; } = Meter.CreateCounter<long>("servicebus.messages.send.sent", unit: "{message}", description: "Number of Azure Service Bus messages sent successfully.");

    /// <summary>
    /// Gets the counter that tracks the number of Azure Service Bus messages sent successfully.
    /// </summary>
    public static Counter<long> MessagesSendFailed { get; } = Meter.CreateCounter<long>("servicebus.messages.send.failed", unit: "{message}", description: "Number of Azure Service Bus messages that failed to send.");

    /// <summary>
    /// Gets the histogram that tracks the duration, in milliseconds, of Azure Service Bus message send operations, regardless of success or failure.
    /// </summary>
    public static Histogram<double> MessagesSendDuration { get; } = Meter.CreateHistogram<double>("servicebus.messages.send.duration", unit: "ms", description: "Duration of Azure Service Bus messages send (success or failure).");

    /// <summary>
    /// Gets the counter that tracks the number of Azure Service Bus messages received and completed.
    /// </summary>
    public static Counter<long> MessagesReceivedComplete { get; } = Meter.CreateCounter<long>("servicebus.messages.received.completed", unit: "{message}", description: "Number of Azure Service Bus messages received and completed.");

    /// <summary>
    /// Gets the counter that tracks the number of Azure Service Bus messages received and dead-lettered.
    /// </summary>
    public static Counter<long> MessagesReceivedDeadLetter { get; } = Meter.CreateCounter<long>("servicebus.messages.received.deadlettered", unit: "{message}", description: "Number of Azure Service Bus messages received and dead-lettered.");

    /// <summary>
    /// Gets the counter that tracks the number of Azure Service Bus messages received and abandoned.
    /// </summary>
    public static Counter<long> MessagesReceivedAbandoned { get; } = Meter.CreateCounter<long>("servicebus.messages.received.abandoned", unit: "{message}", description: "Number of Azure Service Bus messages received and abandoned.");

    /// <summary>
    /// Gets the histogram that tracks the duration, in milliseconds, of Azure Service Bus message receive operations, regardless of success or failure.
    /// </summary>
    public static Histogram<double> MessagesReceivedDuration { get; } = Meter.CreateHistogram<double>("servicebus.messages.received.duration", unit: "ms", description: "Duration of Azure Service Bus messages receive processing (success or failure).");

    /// <summary>
    /// Gets the histogram that tracks the lag duration (now - enqueued time), in milliseconds, of Azure Service Bus message receive operations, regardless of success or failure.
    /// </summary>
    public static Histogram<double> MessagesReceivedLagDuration { get; } = Meter.CreateHistogram<double>("servicebus.messages.received.lag_duration", unit: "ms", description: "Lag duration (now - enqueued time) of Azure Service Bus messages receive processing (success or failure).");
}