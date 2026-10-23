namespace Contoso.Products.Test.Unit.Search;

public class ProductSearchDocumentTests : WithGenericTester<EntryPoint>
{
    [Test]
    public void Create_NormalizesContentAndSortsTags() => Test.Scoped(test => test.Run(_ =>
    {
        var product = new Product { Sku = " test-1 ", Text = " Bike \t cleaner ", Description = " Removes \n dirt and mud. ", BrandCode = "MUCOFF", CategoryCode = "A", SubCategoryCode = "CL", Tags = ["mud", " cleaning ", "mud", ""] };
        var document = ProductSearchDocument.Create(product);

        document.Text.Should().Be("Name: Bike cleaner\nSKU: TEST-1\nDescription: Removes dirt and mud.\nBrand: Muc-Off\nCategory: Accessories\nSubcategory: Cleaner\nTags: cleaning, mud");
        document.SourceHash.Should().HaveLength(64);
        ProductSearchDocument.Create(product).Should().Be(document);
    }).AssertSuccess());

    [Test]
    public void Create_DescriptionChangeChangesHash() => Test.Scoped(test => test.Run(_ =>
    {
        var product = new Product { Sku = "test-1", Text = "Cleaner" };
        var before = ProductSearchDocument.Create(product);
        product.Description = "A cleaner for removing mud from bicycles.";
        ProductSearchDocument.Create(product).SourceHash.Should().NotBe(before.SourceHash);
        var described = ProductSearchDocument.Create(product);
        product.Description = " A cleaner for removing \n mud from bicycles. ";
        ProductSearchDocument.Create(product).Should().Be(described);
    }).AssertSuccess());

    [Test]
    public void Create_ReorderedTagsAndNonSearchChangesDoNotChangeHash() => Test.Scoped(test => test.Run(_ =>
    {
        var product = new Product { Sku = "test-1", Text = "Cleaner", Tags = ["mud", "cleaning"], Price = 10 };
        var before = ProductSearchDocument.Create(product);
        product.Tags = ["cleaning", "mud", "mud"];
        product.Price = 99;
        product.IsInactive = true;
        product.ETag = "updated";
        ProductSearchDocument.Create(product).Should().Be(before);
    }).AssertSuccess());

    [Test]
    public void Create_ChangedTextOrReferenceDataChangesHash() => Test.Scoped(test => test.Run(_ =>
    {
        var product = new Product { Sku = "test-1", Text = "Cleaner", BrandCode = "MUCOFF" };
        var before = ProductSearchDocument.Create(product);
        product.BrandCode = "SHIMANO";
        ProductSearchDocument.Create(product).SourceHash.Should().NotBe(before.SourceHash);
        product.BrandCode = "MUCOFF";
        product.Text = "Lubricant";
        ProductSearchDocument.Create(product).SourceHash.Should().NotBe(before.SourceHash);
    }).AssertSuccess());

    [TestCase(null)]
    [TestCase("")]
    [TestCase("  ")]
    public void Create_MissingReferenceDataDoesNotInventLabels(string? code) => Test.Scoped(test => test.Run(_ =>
    {
        var product = new Product { Sku = "test-1", Text = "Cleaner", BrandCode = code, CategoryCode = code, SubCategoryCode = code };
        ProductSearchDocument.Create(product).Text.Should().Contain("Brand: \nCategory: \nSubcategory: \n");
    }).AssertSuccess());

    [Test]
    public void Create_UnknownReferenceDataFallsBackToCodes() => Test.Scoped(test => test.Run(_ =>
    {
        var product = new Product { Sku = "test-1", Text = "Cleaner", BrandCode = "UNKNOWN", CategoryCode = "UNKNOWN", SubCategoryCode = "UNKNOWN" };
        ProductSearchDocument.Create(product).Text.Should().Contain("Brand: UNKNOWN\nCategory: UNKNOWN\nSubcategory: UNKNOWN\n");
    }).AssertSuccess());

    [TestCase(null)]
    [TestCase("")]
    [TestCase("  ")]
    public void Create_MissingProductNameIsRejected(string? text)
    {
        Action action = () => ProductSearchDocument.Create(new Product { Sku = "test-1", Text = text });
        action.Should().Throw<ArgumentException>();
    }
}
