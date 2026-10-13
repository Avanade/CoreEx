namespace Contoso.Products.Test.Unit.Validators;

public class ProductValidatorTests : WithGenericTester<EntryPoint>
{
    [Test]
    public void Empty_Required() => Test.Scoped(test =>
    {
        var p = new Product();
        ProductValidator.Default.AssertErrors(p,
            ("sku", "Sku is required."),
            ("text", "Text is required."),
            ("subCategory", "Sub-category is required."),
            ("unitOfMeasure", "Unit-of-measure is required."));
    });

    [Test]
    public void Invalid_ReferenceData() => Test.Scoped(test =>
    {
        var p = new Product { Sku = "X", Text = "Test", SubCategoryCode = "XX", UnitOfMeasureCode = "XX", Price = -9.99m };
        ProductValidator.Default.AssertErrors(p,
            ("subCategory", "Sub-category is invalid."),
            ("unitOfMeasure", "Unit-of-measure is invalid."),
            ("price", "Price must be greater than or equal to zero."));
    });

    [Test]
    public void Success() => Test.Scoped(test =>
    {
        var p = new Product { Sku = "X", Text = "Test", SubCategoryCode = "XC", UnitOfMeasureCode = "EA", Price = 9.99m };
        ProductValidator.Default.AssertSuccess(p);
    });

    [Test]
    public void Description_TooLong() => Test.Scoped(test =>
    {
        var p = new Product { Sku = "X", Text = "Test", SubCategoryCode = "XC", UnitOfMeasureCode = "EA", Description = new string('x', 2001) };
        ProductValidator.Default.AssertErrors(p, ("description", "Description must not exceed 2000 character(s) in length."));
    });

    [Test]
    public void Description_MaximumLength() => Test.Scoped(test =>
    {
        var p = new Product { Sku = "X", Text = "Test", SubCategoryCode = "XC", UnitOfMeasureCode = "EA", Description = new string('x', 2000) };
        ProductValidator.Default.AssertSuccess(p);
    });

    [TestCase(null)]
    [TestCase("A cross-country mountain bicycle.")]
    public void Description_Optional(string? description) => Test.Scoped(test =>
    {
        var p = new Product { Sku = "X", Text = "Test", SubCategoryCode = "XC", UnitOfMeasureCode = "EA", Description = description };
        ProductValidator.Default.AssertSuccess(p);
    });
}
