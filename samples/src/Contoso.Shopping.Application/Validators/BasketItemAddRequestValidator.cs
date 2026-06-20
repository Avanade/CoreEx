namespace Contoso.Shopping.Application.Validators;

public class BasketItemAddRequestValidator : Validator<BasketItemAddRequest, BasketItemAddRequestValidator>
{
    public BasketItemAddRequestValidator()
    {
        Property(x => x.ProductId).Mandatory();
        Property(x => x.Quantity).GreaterThanOrEqualTo(0);
    }
}