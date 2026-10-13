namespace Contoso.Products.Test.Api;

public partial class ProductMutateTests : WithApiTester<Contoso.Products.Api.Program>
{
    [Test]
    public void Patch_NotFound()
    {
        // Arrange/Act/Assert.
        Test.Http()
            .Run(HttpMethod.Patch, "/api/products/404", new { text = "abc" }, requestModifier: r => r.WithMergePatchJsonContentType())
            .AssertNotFound();
    }

    [Test]
    public void Patch_Concurrency()
    {
        // Arrange.
        var p = Test.Http<Product>()
            .Run(HttpMethod.Get, $"/api/products/{7.ToGuid()}")
            .AssertOK()
            .Value!;

        p.Text += " Patched";

        // Act/Assert.
        Test.Http()
            .Run(HttpMethod.Patch, $"/api/products/{p.Id}", new { text = p.Text }, requestModifier: r => r.WithIfMatch("AAAAAAAA").WithMergePatchJsonContentType())
            .AssertPreconditionFailed();
    }

    [Test]
    public void Patch_Validation()
    {
        // Arrange.
        var p = Test.Http<Product>()
            .Run(HttpMethod.Get, $"/api/products/{7.ToGuid()}")
            .AssertOK()
            .Value!;

        p.Text += " Patched";

        // Act/Assert.
        Test.Http()
            .Run(HttpMethod.Patch, $"/api/products/{p.Id}", new { text = p.Text, UnitOfMeasure = "XX" }, requestModifier: r => r.WithIfMatch(p.ETag).WithMergePatchJsonContentType())
            .AssertBadRequest()
            .AssertErrors("Unit-of-measure is invalid.");
    }

    [Test]
    public void Patch_Success()
    {
        // Arrange.
        var p = Test.Http<Product>()
            .Run(HttpMethod.Get, $"/api/products/{7.ToGuid()}")
            .AssertOK()
            .Value!;

        p.Text += " Patched";
        p.Description = "A full-suspension carbon mountain bicycle for technical trail riding.";

        // Act/Assert.
        var u = Test.Http<Product>()
            .ExpectPostgresOutboxEvents(e => e.AssertWithValue("contoso", "contoso.products.product.updated.v1"))
            .Run(HttpMethod.Patch, $"/api/products/{p.Id}", new { text = p.Text, description = p.Description }, requestModifier: r => r.WithIfMatch(p.ETag).WithMergePatchJsonContentType())
            .AssertOK()
            .AssertValue(p, "etag", "changelog")
            .Value!;

        u.Text.Should().Be(p.Text);
        u.Description.Should().Be(p.Description);
        u.ETag.Should().NotBe(p.ETag);

        // Assert.
        Test.Http<Product>()
            .Run(HttpMethod.Get, $"/api/products/{p.Id}")
            .AssertOK()
            .AssertValue(u, "etag", "changelog");
    }

    [Test]
    public void Patch_ClearDescription()
    {
        var product = Test.Http<Product>()
            .Run(HttpMethod.Get, $"/api/products/{30.ToGuid()}")
            .AssertOK()
            .Value!;
        product.Description.Should().NotBeNullOrEmpty();

        var updated = Test.Http<Product>()
            .ExpectPostgresOutboxEvents(e => e.AssertWithValue("contoso", "contoso.products.product.updated.v1"))
            .Run(HttpMethod.Patch, $"/api/products/{product.Id}", requestModifier: r =>
            {
                r.Content = new StringContent("{\"description\":null}", System.Text.Encoding.UTF8, "application/merge-patch+json");
                r.WithIfMatch(product.ETag);
            })
            .AssertOK()
            .Value!;

        updated.Description.Should().BeNull();
        updated.Text.Should().Be(product.Text);
        updated.ETag.Should().NotBe(product.ETag);
        Test.Http<Product>()
            .Run(HttpMethod.Get, $"/api/products/{product.Id}")
            .AssertOK()
            .AssertValue(updated);
    }

    [Test]
    public void Patch_NoChanges()
    {
        // Arrange.
        var p = Test.Http<Product>()
            .Run(HttpMethod.Get, $"/api/products/{8.ToGuid()}")
            .AssertOK()
            .Value!;

        // Act/Assert.
        var u = Test.Http<Product>()
            .ExpectNoSqlServerOutboxEvents()
            .Run(HttpMethod.Patch, $"/api/products/{p.Id}", new { }, requestModifier: r => r.WithIfMatch(p.ETag).WithMergePatchJsonContentType())
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
            .AssertValue(u, "etag", "changelog");
    }
}
