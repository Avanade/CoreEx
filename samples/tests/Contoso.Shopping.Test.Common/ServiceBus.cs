using Azure.Messaging.ServiceBus.Administration;

namespace Contoso.Shopping.Test.Common;

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
            new CreateSubscriptionOptions("contoso", "products") { RequiresSession = true }
        ])
    ];
}
