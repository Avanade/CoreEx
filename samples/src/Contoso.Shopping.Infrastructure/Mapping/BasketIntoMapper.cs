namespace Contoso.Shopping.Infrastructure.Mapping;

internal sealed class BasketIntoMapper : IntoMapper<Domain.Basket, Persistence.Basket, BasketIntoMapper>
{
    protected override void OnMapInto(Domain.Basket source, Persistence.Basket destination)
    {
        destination.Id = source.Id;
        destination.CustomerId = source.CustomerId;
        destination.BasketStatusCode = source.Status;
        destination.SubTotal = source.SubTotal;
        destination.DiscountCouponCode = source.DiscountCoupon?.Code;
        destination.DiscountAmount = source.DiscountAmount;
        destination.Total = source.Total;
    }
}