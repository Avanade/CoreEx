namespace app-name.Subscriber.Subscribers;

/// <summary>Represents a placeholder subscriber for bootstrapping.</summary>
/// <remarks>Replace this with your actual event subscribers.</remarks>
[ScopedService]
public class PlaceholderSubscriber : SubscribedBase
{
    /// <inheritdoc/>
    protected override Task<Result> OnReceiveAsync(EventData @event, EventSubscriberArgs args, CancellationToken cancellationToken = default)
    {
        // This is a placeholder subscriber. Replace with your actual event handling logic.
        return Task.FromResult(Result.Success());
    }
}
