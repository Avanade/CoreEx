namespace Contoso.Shopping.Infrastructure.Mapping;

internal sealed class BasketItemIntoMapper : IntoMapper<Domain.BasketItem, Persistence.BasketItem, BasketItemIntoMapper>
{
    protected override void OnMapInto(Domain.BasketItem source, Persistence.BasketItem destination)
    {
        destination.Id = source.Id;
        destination.ProductId = source.ProductId;
        destination.Sku = source.Sku;
        destination.Text = source.Text;
        destination.UnitOfMeasureCode = source.Pricing.UnitOfMeasure;
        destination.Quantity = source.Pricing.Quantity;
        destination.UnitPrice = source.Pricing.UnitPrice;
    }
}