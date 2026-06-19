namespace Contoso.Shopping.Test.Unit.Validators;

public class BasketItemUpdateRequestValidatorTests : WithGenericTester<EntryPoint>
{
    [Test]
    public void BasketItemUpdateRequest_Validate_Quantity_Negative() => Test.Scoped(test =>
    {
        var r = new BasketItemUpdateRequest { Quantity = -1m };
        BasketItemUpdateRequestValidator.Default.AssertErrors(r,
            ("quantity", "Quantity must be greater than or equal to '0'."));
    });

    [Test]
    public void BasketItemUpdateRequest_Validate_Success() => Test.Scoped(test =>
    {
        var r = new BasketItemUpdateRequest { Quantity = 2m };
        BasketItemUpdateRequestValidator.Default.AssertSuccess(r);
    });
}
