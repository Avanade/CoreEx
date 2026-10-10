namespace Contoso.Shopping.Infrastructure.Mapping;

/// <summary>
/// Maps the <see cref="Persistence.Basket"/> to a summary (list-oriented) <see cref="Contracts.Basket"/>; i.e. excludes the <see cref="Contracts.Basket.Items"/> and <see cref="Contracts.Basket.ShippingAddress"/>.
/// </summary>
public class BasketSummaryMapper : Mapper<Persistence.Basket, Contracts.Basket, BasketSummaryMapper>
{
    protected override Contracts.Basket OnMap(Persistence.Basket source) => new()
    {
        Id = source.Id,
        CustomerId = source.CustomerId,
        StatusCode = source.BasketStatusCode,
        Pricing = new Contracts.BasketPricing
        {
            SubTotal = source.SubTotal,
            DiscountCouponCode = source.DiscountCouponCode,
            DiscountAmount = source.DiscountAmount,
            Total = source.Total
        },
        ChangeLog = ChangeLog.CreateFrom(source),
        ETag = source.ETag
    };
}
