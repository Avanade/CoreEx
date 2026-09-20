namespace Contoso.Customers.Test.Api;

public partial class CustomerMutateTests : WithApiTester<Contoso.Customers.Api.Program>
{
    [Test]
    public void Update_NotFound()
    {
        // Arrange.
        var c = Test.Http<Customer>()
            .Run(HttpMethod.Get, $"/api/customers/{1.ToGuid()}")
            .AssertOK()
            .Value!;

        // Act/Assert.
        Test.Http()
            .Run(HttpMethod.Put, "/api/customers/404", c)
            .AssertNotFound();
    }

    [Test]
    public void Update_Concurrency()
    {
        // Arrange.
        var c = Test.Http<Customer>()
            .Run(HttpMethod.Get, $"/api/customers/{4.ToGuid()}")
            .AssertOK()
            .Value!;

        c.LastName += " Updated";

        // Act/Assert.
        Test.Http()
            .Run(HttpMethod.Put, $"/api/customers/{c.Id}", c, requestModifier: r => r.WithIfMatch("AAAAAAAA"))
            .AssertPreconditionFailed();
    }

    [Test]
    public void Update_Success()
    {
        // Arrange.
        var c = Test.Http<Customer>()
            .Run(HttpMethod.Get, $"/api/customers/{1.ToGuid()}")
            .AssertOK()
            .Value!;

        c.LastName += " Updated";

        // Act/Assert.
        var u = Test.Http<Customer>()
            .ExpectIdentifier()
            .ExpectETag()
            .ExpectChangeLogUpdated()
            .ExpectValue(c)
            .ExpectCosmosDbOutboxEvents(e => e.AssertWithValue("contoso", "contoso.customers.customer.updated.v1"))
            .Run(HttpMethod.Put, $"/api/customers/{c.Id}", c)
            .AssertOK()
            .Value!;

        u.LastName.Should().Be(c.LastName);
        u.ETag.Should().NotBe(c.ETag);

        // Assert.
        Test.Http<Customer>()
            .Run(HttpMethod.Get, $"/api/customers/{c.Id}")
            .AssertOK()
            .AssertValue(u);
    }
}
