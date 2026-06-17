namespace Contoso.Shopping.Infrastructure.Mapping;

public class ProductMapper : BiDirectionMapper<Product, Persistence.Product, ProductMapper>
{
    protected override Persistence.Product OnMap(Product source) => new()
    {
        Id = source.Id!,
        Sku = source.Sku!,
        Text = source.Text!,
        UnitOfMeasureCode = source.UnitOfMeasureCode!,
        Price = source.Price,
        IsInactive = source.IsInactive,
        IsNonStocked = source.IsNonStocked
    };

    protected override Product OnMap(Persistence.Product source) => new()
    {
        Id = source.Id,
        Sku = source.Sku,
        Text = source.Text,
        UnitOfMeasureCode = source.UnitOfMeasureCode,
        Price = source.Price,
        IsInactive = source.IsInactive,
        IsNonStocked = source.IsNonStocked
    };
}