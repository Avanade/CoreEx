namespace Contoso.Products.Test.Api;

public partial class ProductMutateTests : WithApiTester<Contoso.Products.Api.Program>
{
    [Test]
    public void Activate_NotFound()
    {
        // Arrange/Act/Assert.
        Test.Http()
            .ExpectNoPostgresOutboxEvents()
            .Run(HttpMethod.Post, "/api/products/404/activate")
            .AssertNotFound();
    }

    [Test]
    public void Activate_AlreadyActive()
    {
        var id = 20.ToGuid().ToString();

        // Arrange/Act/Assert - no-op, so unchanged and no event.
        var p = Test.Http<Product>()
            .ExpectNoPostgresOutboxEvents()
            .Run(HttpMethod.Post, $"/api/products/{id}/activate")
            .AssertOK()
            .Value!;

        p.IsInactive.Should().BeFalse();
    }

    [Test]
    public void Activate_Success()
    {
        var id = 27.ToGuid().ToString();

        // Arrange.
        Test.Http<Product>()
            .Run(HttpMethod.Get, $"/api/products/{id}")
            .AssertOK()
            .Value!.IsInactive.Should().BeTrue();

        // Act.
        var p = Test.Http<Product>()
            .ExpectPostgresOutboxEvents(c => c.AssertMetadata("contoso", "contoso.products.product.activated.v1", id))
            .Run(HttpMethod.Post, $"/api/products/{id}/activate")
            .AssertOK()
            .Value!;

        p.IsInactive.Should().BeFalse();

        // Assert persisted.
        Test.Http<Product>()
            .Run(HttpMethod.Get, $"/api/products/{id}")
            .AssertOK()
            .Value!.IsInactive.Should().BeFalse();

        // Assert idempotent - already active, so no event.
        Test.Http<Product>()
            .ExpectNoPostgresOutboxEvents()
            .Run(HttpMethod.Post, $"/api/products/{id}/activate")
            .AssertOK()
            .Value!.IsInactive.Should().BeFalse();
    }
}
