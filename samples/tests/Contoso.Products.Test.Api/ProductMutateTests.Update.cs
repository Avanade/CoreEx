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
        u.ETag.Should().NotBe(p.ETag);

        // Assert.
        Test.Http<Product>()
            .Run(HttpMethod.Get, $"/api/products/{p.Id}")
            .AssertOK()
            .AssertValue(u);
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