namespace Contoso.Customers.Test.Api;

public partial class CustomerMutateTests : WithApiTester<Contoso.Customers.Api.Program>
{
    [Test]
    public void Delete_NotFound()
    {
        // Arrange/Act/Assert.
        Test.Http()
            .Run(HttpMethod.Delete, "/api/customers/404")
            .AssertNoContent();
    }

    [Test]
    public void Delete_HasShopped()
    {
        // Arrange/Act/Assert.
        Test.Http()
            .Run(HttpMethod.Delete, $"/api/customers/{2.ToGuid()}")
            .AssertBadRequest()
            .AssertProblemDetails(p => p.Title.Should().Be("A customer that has already shopped cannot be deleted."));
    }

    [Test]
    public void Delete_Success()
    {
        var id = 3.ToGuid().ToString();

        // Arrange.
        Test.Http()
            .Run(HttpMethod.Get, $"/api/customers/{id}")
            .AssertOK();

        // Act.
        Test.Http()
            .ExpectCosmosDbOutboxEvents(c => c.AssertMetadata("contoso", "contoso.customers.customer.deleted", id))
            .Run(HttpMethod.Delete, $"/api/customers/{id}")
            .AssertNoContent();

        // Assert idempotent.
        Test.Http()
            .Run(HttpMethod.Delete, $"/api/customers/{id}")
            .AssertNoContent();

        // Assert.
        Test.Http()
            .Run(HttpMethod.Get, $"/api/customers/{id}")
            .AssertNotFound();
    }
}
