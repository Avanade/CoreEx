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

        // Act/Assert.
        var u = Test.Http<Product>()
            .ExpectPostgresOutboxEvents(e => e.AssertWithValue("contoso", "contoso.products.product.updated.v1"))
            .Run(HttpMethod.Patch, $"/api/products/{p.Id}", new { text = p.Text }, requestModifier: r => r.WithIfMatch(p.ETag).WithMergePatchJsonContentType())
            .AssertOK()
            .AssertValue(p, "etag", "changelog")
            .Value!;

        u.Text.Should().Be(p.Text);
        u.ETag.Should().NotBe(p.ETag);

        // Assert.
        Test.Http<Product>()
            .Run(HttpMethod.Get, $"/api/products/{p.Id}")
            .AssertOK()
            .AssertValue(u, "etag", "changelog");
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