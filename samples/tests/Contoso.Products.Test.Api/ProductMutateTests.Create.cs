namespace Contoso.Products.Test.Api;

public partial class ProductMutateTests : WithApiTester<Contoso.Products.Api.Program>
{
    [Test]
    public void Create_Empty()
    {
        // Act/Assert.
        Test.Http()
            .Run(HttpMethod.Post, "/api/products", new Product())
            .AssertBadRequest()
            .AssertErrors(
                "Sku is required.",
                "Text is required.",
                "Unit-of-measure is required.",
                "Sub-category is required."
            );
    }

    [Test]
    public void Create_Bad_Data()
    {
        // Arrange.
        var p = new Product
        {
            Sku = "abc",
            Text = null,
            Price = -1.99M,
            SubCategoryCode = "XX",
            BrandCode = "yeti",
        };

        // Act/Assert.
        Test.Http()
            .Run(HttpMethod.Post, "/api/products", p)
            .AssertBadRequest()
            .AssertErrors(
                "Text is required.",
                "Unit-of-measure is required.",
                "Price must be greater than or equal to zero.",
                "Sub-category is invalid."
            );
    }

    [Test]
    public void Create_Duplicate()
    {
        // Arrange.
        var p = new Product
        {
            Sku = "Yeti-ASR-c2-2025",
            Text = "Yeti ASR C2",
            Price = 5800M,
            SubCategoryCode = "XC",
            UnitOfMeasureCode = "EA",
            BrandCode = "YETI"
        };

        // Act/Assert.
        Test.Http()
            .Run(HttpMethod.Post, "/api/products", p)
            .AssertConflict();
    }

    [Test]
    public void Create_Success()
    {
        // Arrange.
        var p = new Product
        {
            Sku = "New-SKU-123",
            Text = "New Product",
            Price = 1000M,
            SubCategoryCode = "XC",
            UnitOfMeasureCode = "EA",
            BrandCode = "YETI"
        };

        // Act/Assert.
        var r = Test.Http<Product>()
            .ExpectIdentifier()
            .ExpectETag()
            .ExpectChangeLogCreated()
            .ExpectJsonFromResource("ProductMutateTests.Create_Success.res.json")
            .ExpectPostgresOutboxEvents(e => e.AssertWithValue("contoso", "contoso.products.product.created.v1"))
            .Run(HttpMethod.Post, "/api/products", p)
            .AssertCreated()
            .AssertLocationHeader(r => new Uri($"/api/products/{r!.Id}", UriKind.Relative))
            .Value!;

        // Assert.
        Test.Http<Product>()
            .Run(HttpMethod.Get, $"/api/products/{r.Id}")
            .AssertOK()
            .AssertValue(r);
    }

    [Test]
    public void Create_WithTags()
    {
        // Arrange — create a product with tags to verify the JSONB column round-trips correctly.
        var p = new Product
        {
            Sku = "TAGGED-SKU-001",
            Text = "Tagged Product",
            Price = 1500M,
            SubCategoryCode = "XC",
            UnitOfMeasureCode = "EA",
            BrandCode = "YETI",
            Tags = ["cross-country", "carbon", "race"]
        };

        // Act/Assert — create and verify the response includes tags.
        var r = Test.Http<Product>()
            .ExpectIdentifier()
            .ExpectETag()
            .ExpectChangeLogCreated()
            .ExpectPostgresOutboxEvents(e => e.AssertWithValue("contoso", "contoso.products.product.created.v1"))
            .Run(HttpMethod.Post, "/api/products", p)
            .AssertCreated()
            .AssertLocationHeader(r => new Uri($"/api/products/{r!.Id}", UriKind.Relative))
            .AssertJsonFromResource("ProductMutateTests.Create_WithTags.res.json", "id", "etag", "changeLog")
            .Value!;

        // Assert tags survive a Get round-trip.
        r.Tags.Should().BeEquivalentTo(["cross-country", "carbon", "race"]);
        Test.Http<Product>()
            .Run(HttpMethod.Get, $"/api/products/{r.Id}")
            .AssertOK()
            .AssertValue(r);
    }

    [Test]
    public void Create_IdempotencyKey()
    {
        // Arrange.
        var p = new Product
        {
            Sku = "New-SKU-456",
            Text = "Another New Product",
            Price = 1200M,
            SubCategoryCode = "XC",
            UnitOfMeasureCode = "EA",
            BrandCode = "YETI"
        };

        var ik = Guid.NewGuid().ToString();

        // Act/Assert.
        var v1 = Test.Http<Product>()
            .ExpectPostgresOutboxEvents()
            .Run(HttpMethod.Post, "/api/products", p, requestModifier: r => r.WithIdempotencyKey(ik))
            .AssertCreated()
            .AssertLocationHeader(r => new Uri($"/api/products/{r!.Id}", UriKind.Relative))
            .Value!;

        // Assert: repeat with same idempotency key; should get back same result & no extra event emitted.
        var v2 = Test.Http<Product>()
            .ExpectNoSqlServerOutboxEvents()
            .Run(HttpMethod.Post, "/api/products", p, requestModifier: r => r.WithIdempotencyKey(ik))
            .AssertCreated()
            .AssertLocationHeader(r => new Uri($"/api/products/{r!.Id}", UriKind.Relative))
            .Value!;

        // Assert: both results are the same.
        ObjectComparer.Assert(v1, v2);
    }
}
