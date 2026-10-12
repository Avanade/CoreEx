namespace app-name.Test.Common;

/// <summary>
/// Defines the Azure Service Bus entities (queues, topics and subscriptions) for the 'domain-name' domain that the integration tests reset to a known state via <c>Test.ResetAzureServiceBusAsync</c>; this is the
/// code-based configuration and is therefore the place to add entities (the emulator <c>servicebus/Config.json</c> is intentionally empty).
/// </summary>
public static class ServiceBus
{
    /// <summary>
    /// Gets the queues; none by default. Add a session-enabled queue named '<c>domain-parent-lower-{domain}</c>' for each domain that is to receive commands (see <c>NamedDestinationProvider</c>).
    /// </summary>
    public static CreateQueueOptions[]? GetQueues() => [];

    /// <summary>
    /// Gets the topics and their subscriptions; a single shared topic with a session-enabled subscription for this domain.
    /// </summary>
    public static (CreateTopicOptions Topic, CreateSubscriptionOptions[] Subscriptions)[]? GetTopicsAndSubscriptions() =>
    [
        (new CreateTopicOptions("domain-parent-lower"),
        [
            new CreateSubscriptionOptions("domain-parent-lower", "domain-name-lower") { RequiresSession = true }
        ])
    ];
}