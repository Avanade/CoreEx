namespace Contoso.Products.Application.Validators;

public class ProductValidator : Validator<Contracts.Product, ProductValidator>
{
    public ProductValidator()
    {
        Property(p => p.Sku).Mandatory().MaximumLength(50);
        Property(p => p.Text).Mandatory().MaximumLength(250);
        Property(p => p.SubCategory).Mandatory().IsValid();
        Property(p => p.UnitOfMeasure).Mandatory().IsValid();
        Property(p => p.Price).PrecisionScale(null, 2).GreaterThanOrEqualTo(0, _ => "zero");
    }
}