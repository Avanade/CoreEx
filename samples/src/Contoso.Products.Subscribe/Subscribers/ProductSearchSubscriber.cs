namespace Contoso.Products.Subscribe.Subscribers;

/// <summary>Reconciles all product lifecycle notifications against authoritative state, including logical deletion.</summary>
[ScopedService, Subscribe("contoso.products.product.**")]
public sealed class ProductSearchSubscriber(IProductSearchSyncAdapter adapter) : SubscribedBase
{
    private readonly IProductSearchSyncAdapter _adapter = adapter.ThrowIfNull();

    /// <inheritdoc/>
    protected override Task<Result> OnReceiveAsync(EventData @event, EventSubscriberArgs args, CancellationToken cancellationToken = default)
        => _adapter.SynchronizeAsync(@event.Key.Required(), cancellationToken);
}
