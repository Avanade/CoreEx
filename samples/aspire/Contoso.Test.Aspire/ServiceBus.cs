namespace Contoso.Test.Aspire;

public static class ServiceBus
{
    public static CreateQueueOptions[]? GetQueues() =>
    [
        new CreateQueueOptions("contoso-products") { RequiresSession = true }
    ];

    public static (CreateTopicOptions Topic, CreateSubscriptionOptions[] Subscriptions)[]? GetTopicsAndSubscriptions() =>
    [
        (new CreateTopicOptions("contoso"),
        [
            new CreateSubscriptionOptions("contoso", "shopping") { RequiresSession = true },
            new CreateSubscriptionOptions("contoso", "products") { RequiresSession = true },
            new CreateSubscriptionOptions("contoso", Contoso.Customers.Test.Common.ServiceBus.ObservationSubscription) { RequiresSession = true }
        ])
    ];
}
