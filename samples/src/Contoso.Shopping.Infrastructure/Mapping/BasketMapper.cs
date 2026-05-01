namespace Contoso.Shopping.Infrastructure.Mapping;

public class BasketMapper : Mapper<Persistence.Basket, Domain.Basket, BasketMapper>
{
    protected override Domain.Basket OnMap(Persistence.Basket source)
    {
        var items = source.Items?.Select(i => Domain.BasketItem.CreateFrom(
            i.Id,
            i.ProductId,
            i.Sku,
            i.Text,
            new Domain.ValueObjects.ItemPricing
            {
                UnitOfMeasure = i.UnitOfMeasureCode,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            },
            i.ETag));

        return Domain.Basket.CreateFrom(
            source.Id,
            source.CustomerId,
            source.BasketStatusCode,
            source.DiscountCouponCode,
            items,
            ChangeLog.CreateFrom(source),
            source.ETag);
    }
}