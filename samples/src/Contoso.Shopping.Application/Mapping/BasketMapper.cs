namespace Contoso.Shopping.Application.Mapping;

public class BasketMapper : Mapper<Domain.Basket, Contracts.Basket, BasketMapper>
{
    protected override Contracts.Basket OnMap(Domain.Basket source) => new()
    {
        Id = source.Id,
        CustomerId = source.CustomerId,
        StatusCode = source.Status,
        Pricing = new BasketPricing
        {
            SubTotal = source.SubTotal,
            DiscountCouponCode = source.DiscountCoupon,
            DiscountPercentage = source.DiscountPercentage,
            DiscountAmount = source.DiscountAmount,
            Total = source.Total
        },
        Items = [.. source.Items.Select(i => BasketItemMapper.Map(i))]
    };

    private class BasketItemMapper : Mapper<Domain.BasketItem, Contracts.BasketItem, BasketItemMapper>
    {
        protected override BasketItem OnMap(Domain.BasketItem i) => new()
        {
            Id = i.Id,
            ProductId = i.ProductId,
            Sku = i.Sku,
            Text = i.Text,
            Quantity = i.Pricing.Quantity,
            UnitOfMeasureCode = i.Pricing.UnitOfMeasure,
            UnitPrice = i.Pricing.UnitPrice,
            Total = i.Pricing.Total
        };
    }
}