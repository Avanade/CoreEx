namespace Contoso.Shopping.Application.Validators;

/// <summary>
/// Product validator leveraging the FluentValidation API compatibility; still leverages CoreEx.Validation to perform the actual validation and error handling, but provides the FluentValidation API for
/// ease of use and familiarity.
/// </summary>
public class ProductValidator : AbstractValidator<Product, ProductValidator>
{
    public ProductValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Sku).NotEmpty();
        RuleFor(x => x.Text).NotEmpty();
        RuleFor(x => x.UnitOfMeasure).NotEmpty().IsValid();
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
    }
}