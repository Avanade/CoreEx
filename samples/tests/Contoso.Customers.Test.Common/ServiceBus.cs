namespace Contoso.Customers.Test.Common;

public static class ServiceBus
{
    public const string ObservationSubscription = "customers-relay";

    public static (CreateTopicOptions Topic, CreateSubscriptionOptions[] Subscriptions)[] GetTopicsAndSubscriptions() =>
    [
        (new CreateTopicOptions("contoso"),
        [
            new CreateSubscriptionOptions("contoso", "shopping") { RequiresSession = true },
            new CreateSubscriptionOptions("contoso", "products") { RequiresSession = true },
            new CreateSubscriptionOptions("contoso", ObservationSubscription) { RequiresSession = true }
        ])
    ];

    public static async Task<Dictionary<(string Subject, string Type), ServiceBusReceivedMessage>> ReceiveEventsAsync(ServiceBusClient client, IReadOnlyCollection<(string Subject, string Type)> expected, CancellationToken cancellationToken = default)
    {
        var messages = new Dictionary<(string Subject, string Type), ServiceBusReceivedMessage>();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(90));

        try
        {
            while (expected.Any(key => !messages.ContainsKey(key)))
            {
                ServiceBusSessionReceiver session;
                try
                {
                    session = await client.AcceptNextSessionAsync("contoso", ObservationSubscription, cancellationToken: timeout.Token).ConfigureAwait(false);
                }
                catch (ServiceBusException ex) when (ex.Reason == ServiceBusFailureReason.ServiceTimeout)
                {
                    continue;
                }

                await using (session)
                {
                    while (true)
                    {
                        var batch = await session.ReceiveMessagesAsync(50, TimeSpan.FromSeconds(1), timeout.Token).ConfigureAwait(false);
                        if (batch.Count == 0)
                            break;

                        foreach (var message in batch)
                        {
                            using var json = JsonDocument.Parse(message.Body);
                            var subject = json.RootElement.GetProperty("subject").GetString() ?? throw new InvalidOperationException("A relayed event has no subject.");
                            var type = json.RootElement.GetProperty("type").GetString() ?? throw new InvalidOperationException("A relayed event has no type.");
                            var key = (subject, type);
                            messages.TryAdd(key, message);

                            await session.CompleteMessageAsync(message, timeout.Token).ConfigureAwait(false);
                        }
                    }
                }
            }
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested && timeout.IsCancellationRequested)
        {
            throw new TimeoutException($"Customers relay did not deliver: {string.Join(", ", expected.Where(key => !messages.ContainsKey(key)))}.", ex);
        }

        return messages;
    }
}
