namespace Contoso.Products.Infrastructure.Mapping;

public class ProductMapper : BiDirectionMapper<Contracts.Product, Persistence.Product, ProductMapper>
{
    protected override Persistence.Product OnMap(Contracts.Product source) => new()
    {
        Id = source.Id!,
        Sku = source.Sku!,
        Text = source.Text!,
        SubCategoryCode = source.SubCategory?.Code!,
        UnitOfMeasureCode = source.UnitOfMeasure?.Code!,
        BrandCode = source.Brand?.Code!,
        Price = source.Price,
        IsInactive = source.IsInactive,
        IsNonStocked = source.IsNonStocked
    };

    protected override Contracts.Product OnMap(Persistence.Product source) => new Contracts.Product()
    {
        Id = source.Id,
        Sku = source.Sku,
        Text = source.Text,
        SubCategoryCode = source.SubCategoryCode,
        UnitOfMeasureCode = source.UnitOfMeasureCode,
        BrandCode = source.BrandCode,
        Price = source.Price,
        IsInactive = source.IsInactive,
        IsNonStocked = source.IsNonStocked
    }.Adjust(p => p.CategoryCode = p.SubCategory?.CategoryCode);
}