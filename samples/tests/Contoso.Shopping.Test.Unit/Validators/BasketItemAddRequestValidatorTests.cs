namespace Contoso.Shopping.Test.Unit.Validators;

public class BasketItemAddRequestValidatorTests : WithGenericTester<EntryPoint>
{
    [Test]
    public void BasketItemAddRequest_Validate_Empty_Required() => Test.Scoped(test =>
    {
        var r = new BasketItemAddRequest();
        BasketItemAddRequestValidator.Default.AssertErrors(r,
            ("productId", "Product is required."));
    });

    [Test]
    public void BasketItemAddRequest_Validate_Quantity_Negative() => Test.Scoped(test =>
    {
        var r = new BasketItemAddRequest { ProductId = "P1", Quantity = -1m };
        BasketItemAddRequestValidator.Default.AssertErrors(r,
            ("quantity", "Quantity must be greater than or equal to '0'."));
    });

    [Test]
    public void BasketItemAddRequest_Validate_Success() => Test.Scoped(test =>
    {
        var r = new BasketItemAddRequest { ProductId = "P1", Quantity = 2m };
        BasketItemAddRequestValidator.Default.AssertSuccess(r);
    });
}
