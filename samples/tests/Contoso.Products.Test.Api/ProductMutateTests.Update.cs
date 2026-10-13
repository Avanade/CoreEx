namespace Contoso.Products.Test.Api;

public partial class ProductMutateTests : WithApiTester<Contoso.Products.Api.Program>
{
    [Test]
    public void Update_NotFound()
    {
        // Arrange.
        var p = Test.Http<Product>()
            .Run(HttpMethod.Get, $"/api/products/{6.ToGuid()}")
            .AssertOK()
            .Value;

        // Act/Assert.
        Test.Http()
            .Run(HttpMethod.Put, "/api/products/404", p)
            .AssertNotFound();
    }

    [Test]
    public void Update_Concurrency()
    {
        // Arrange.
        var p = Test.Http<Product>()
            .Run(HttpMethod.Get, $"/api/products/{6.ToGuid()}")
            .AssertOK()
            .Value!;

        p.Text += " Updated";

        // Act/Assert.
        Test.Http()
            .Run(HttpMethod.Put, $"/api/products/{p.Id}", p, requestModifier: r => r.WithIfMatch("AAAAAAAA"))
            .AssertPreconditionFailed();
    }

    [Test]
    public void Update_Success()
    {
        // Arrange.
        var p = Test.Http<Product>()
            .Run(HttpMethod.Get, $"/api/products/{6.ToGuid()}")
            .AssertOK()
            .Value!;

        p.Text += " Updated";
        p.Description = "A full-suspension carbon mountain bicycle for cross-country trails.";

        // Act/Assert.
        var u = Test.Http<Product>()
            .ExpectIdentifier()
            .ExpectETag()
            .ExpectChangeLogUpdated()
            .ExpectValue(p)
            .ExpectPostgresOutboxEvents(e => e.AssertWithValue("contoso", "contoso.products.product.updated.v1"))
            .Run(HttpMethod.Put, $"/api/products/{p.Id}", p)
            .AssertOK()
            .Value!;

        u.Text.Should().Be(p.Text);
        u.Description.Should().Be(p.Description);
        u.ETag.Should().NotBe(p.ETag);

        // Assert.
        Test.Http<Product>()
            .Run(HttpMethod.Get, $"/api/products/{p.Id}")
            .AssertOK()
            .AssertValue(u);
    }

    [Test]
    public void Update_DescriptionOnly()
    {
        var product = Test.Http<Product>()
            .Run(HttpMethod.Get, $"/api/products/{9.ToGuid()}")
            .AssertOK()
            .Value!;
        var etag = product.ETag;
        product.Description = "A carbon mixed-wheel mountain bicycle for bike-park jumps and steep descents.";

        var updated = Test.Http<Product>()
            .ExpectChangeLogUpdated()
            .ExpectPostgresOutboxEvents(e => e.AssertWithValue("contoso", "contoso.products.product.updated.v1"))
            .Run(HttpMethod.Put, $"/api/products/{product.Id}", product)
            .AssertOK()
            .Value!;

        updated.Description.Should().Be(product.Description);
        updated.ETag.Should().NotBe(etag);
        Test.Http<Product>()
            .Run(HttpMethod.Get, $"/api/products/{product.Id}")
            .AssertOK()
            .AssertValue(updated);
    }

    [Test]
    public void Update_NoChanges()
    {
        // Arrange.
        var p = Test.Http<Product>()
            .Run(HttpMethod.Get, $"/api/products/{6.ToGuid()}")
            .AssertOK()
            .Value!;

        // Act/Assert.
        var u = Test.Http<Product>()
            .Run(HttpMethod.Put, $"/api/products/{p.Id}", p)
            .AssertOK()
            .AssertValue(p, "etag", "changelog")
            .Value!;

        u.Text.Should().Be(p.Text);
        u.ETag.Should().Be(p.ETag);
        u.ChangeLog.Should().NotBeNull();
        u.ChangeLog.UpdatedBy.Should().BeNull();
        u.ChangeLog.UpdatedOn.Should().BeNull();

        // Assert.
        Test.Http<Product>()
            .Run(HttpMethod.Get, $"/api/products/{p.Id}")
            .AssertOK()
            .AssertValue(u);
    }
}
