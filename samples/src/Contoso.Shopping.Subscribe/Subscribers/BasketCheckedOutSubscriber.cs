namespace Contoso.Shopping.Subscribe.Subscribers;

[ScopedService]
[Subscribe("contoso.shopping.basket.checkedout.v1")]
public class BasketCheckedOutSubscriber(INotificationAdapter adapter) : SubscribedBase<Basket>
{
    private readonly INotificationAdapter _adapter = adapter.ThrowIfNull();

    protected override Task<Result> OnReceiveAsync(Basket value, EventData @event, EventSubscriberArgs args, CancellationToken cancellationToken = default)
        => _adapter.SendBasketCheckedOutConfirmationAsync(value, cancellationToken);
}
