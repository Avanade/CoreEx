namespace Contoso.Products.Test.Api;

public partial class ProductMutateTests : WithApiTester<Contoso.Products.Api.Program>
{
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
            UnitOfMeasureCode = "ea",
            BrandCode = "yeti"
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
            UnitOfMeasureCode = "ea",
            BrandCode = "yeti"
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
    public void Create_IdempotencyKey()
    {
        // Arrange.
        var p = new Product
        {
            Sku = "New-SKU-456",
            Text = "Another New Product",
            Price = 1200M,
            SubCategoryCode = "XC",
            UnitOfMeasureCode = "ea",
            BrandCode = "yeti"
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