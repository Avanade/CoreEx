namespace Contoso.Shopping.Test.Unit.Validators;

public class ProductValidatorTests : WithGenericTester<EntryPoint>
{
    [Test]
    public void Product_Validate_Empty_AllRequired() => Test.Scoped(test =>
    {
        var p = new Product();
        ProductValidator.Default.AssertErrors(p,
            ("id", "Identifier is required."),
            ("sku", "Sku is required."),
            ("text", "Text is required."),
            ("unitOfMeasure", "Unit of measure is required."));
    });

    [Test]
    public void Product_Validate_Price_Negative() => Test.Scoped(test =>
    {
        var p = new Product { Id = "1", Sku = "WIDGET", Text = "Widget", UnitOfMeasureCode = "EA", Price = -1m };
        ProductValidator.Default.AssertErrors(p,
            ("price", "Price must be greater than or equal to '0'."));
    });

    [Test]
    public void Product_Validate_UnitOfMeasure_Invalid() => Test.Scoped(test =>
    {
        var p = new Product { Id = "1", Sku = "WIDGET", Text = "Widget", UnitOfMeasureCode = "BADCODE", Price = 9.99m };
        ProductValidator.Default.AssertErrors(p,
            ("unitOfMeasure", "Unit of measure is invalid."));
    });

    [Test]
    public void Product_Validate_Success() => Test.Scoped(test =>
    {
        var p = new Product { Id = "1", Sku = "WIDGET", Text = "Widget", UnitOfMeasureCode = "EA", Price = 9.99m };
        ProductValidator.Default.AssertSuccess(p);
    });
}
