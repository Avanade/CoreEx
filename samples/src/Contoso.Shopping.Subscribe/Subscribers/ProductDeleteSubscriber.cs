namespace Contoso.Shopping.Subscribe.Subscribers;

[ScopedService]
[Subscribe("contoso.products.product.deleted")]
public class ProductDeleteSubscriber(IProductSyncAdapter adapter) : SubscribedBase
{
    private readonly IProductSyncAdapter _adapter = adapter.ThrowIfNull();

    protected override Task<Result> OnReceiveAsync(EventData @event, EventSubscriberArgs args, CancellationToken cancellationToken = default)
        => _adapter.DeleteAsync(@event.Key.Required());
}