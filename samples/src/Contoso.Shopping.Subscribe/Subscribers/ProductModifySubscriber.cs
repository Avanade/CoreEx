namespace Contoso.Shopping.Subscribe.Subscribers;

[ScopedService]
[Subscribe("contoso.products.product.created.v1")]
[Subscribe("contoso.products.product.updated.v1")]
public class ProductModifySubscriber(IProductSyncAdapter adapter) : SubscribedBase<Product>
{
    private readonly IProductSyncAdapter _adapter = adapter.ThrowIfNull();

    public override IValidator<Product>? ValueValidator => ProductValidator.Default;

    protected override Task<Result> OnReceiveAsync(Product value, EventData @event, EventSubscriberArgs args, CancellationToken cancellationToken = default)
        => _adapter.ModifyAsync(value);
}