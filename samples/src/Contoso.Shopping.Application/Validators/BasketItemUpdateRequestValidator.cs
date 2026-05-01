namespace Contoso.Shopping.Application.Validators;

public class BasketItemUpdateRequestValidator : Validator<BasketItemUpdateRequest, BasketItemUpdateRequestValidator>
{
    public BasketItemUpdateRequestValidator()
    {
        Property(x => x.Quantity).GreaterThanOrEqualTo(0);
    }
}