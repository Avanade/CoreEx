namespace Contoso.Products.Test.Api;

public partial class ProductMutateTests : WithApiTester<Contoso.Products.Api.Program>
{
    [Test]
    public void Delete_NotFound()
    {
        // Arrange/Act/Assert.
        Test.Http()
            .Run(HttpMethod.Delete, "/api/products/404")
            .AssertNoContent();
    }

    [Test]
    public void Delete_IsDeleted()
    {
        // Arrange/Act/Assert.
        Test.Http()
            .Run(HttpMethod.Delete, $"/api/products/{18.ToGuid()}")
            .AssertNoContent();
    }

    [Test]
    public void Delete_IsActive()
    {
        // Arrange/Act/Assert.
        Test.Http()
            .Run(HttpMethod.Delete, $"/api/products/{12.ToGuid()}")
            .AssertBadRequest()
            .AssertProblemDetailsTitle("A product must first be deactivated before it can be deleted.");
    }

    [Test]
    public void Delete_Success()
    {
        var id = 13.ToGuid().ToString();

        // Arrange.
        Test.Http()
            .Run(HttpMethod.Get, $"/api/products/{id}")
            .AssertOK();

        // Act.
        Test.Http()
            .ExpectPostgresOutboxEvents(c => c.AssertMetadata("contoso", "contoso.products.product.deleted.v1", id))
            .Run(HttpMethod.Delete, $"/api/products/{id}")
            .AssertNoContent();

        // Assert idempotent.
        Test.Http()
            .Run(HttpMethod.Delete, $"/api/products/{id}")
            .AssertNoContent();

        // Assert.
        Test.Http()
            .Run(HttpMethod.Get, $"/api/products/{id}")
            .AssertNotFound();
    }
}