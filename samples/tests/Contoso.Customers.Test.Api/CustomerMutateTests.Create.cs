namespace Contoso.Customers.Test.Api;

public partial class CustomerMutateTests : WithApiTester<Contoso.Customers.Api.Program>
{
    [Test]
    public void Create_Empty()
    {
        // Act/Assert.
        Test.Http()
            .Run(HttpMethod.Post, "/api/customers", new Customer())
            .AssertBadRequest()
            .AssertErrors(
                "First name is required.",
                "Last name is required.",
                "Email is required.",
                "Customer type is required.",
                "Contact method is required."
            );
    }

    [Test]
    public void Create_Bad_Data()
    {
        // Arrange.
        var c = new Customer
        {
            FirstName = "Bart",
            LastName = "Simpson",
            Email = "bart.simpson@example.com",
            CustomerTypeCode = "XX",
            ContactMethodCode = "XX"
        };

        // Act/Assert.
        Test.Http()
            .Run(HttpMethod.Post, "/api/customers", c)
            .AssertBadRequest()
            .AssertErrors(
                "Customer type is invalid.",
                "Contact method is invalid."
            );
    }

    [Test]
    public void Create_Success()
    {
        // Arrange.
        var c = new Customer
        {
            FirstName = "Marge",
            LastName = "Simpson",
            Email = "marge.simpson@example.com",
            CustomerTypeCode = "IND",
            ContactMethodCode = "EM"
        };

        // Act/Assert.
        var r = Test.Http<Customer>()
            .ExpectIdentifier()
            .ExpectETag()
            .ExpectChangeLogCreated()
            .ExpectCosmosDbOutboxEvents(e => e.AssertWithValue("contoso", "contoso.customers.customer.created.v1"))
            .Run(HttpMethod.Post, "/api/customers", c)
            .AssertCreated()
            .AssertLocationHeader(r => new Uri($"/api/customers/{r!.Id}", UriKind.Relative))
            .Value!;

        r.HasShopped.Should().BeFalse();

        // Assert.
        Test.Http<Customer>()
            .Run(HttpMethod.Get, $"/api/customers/{r.Id}")
            .AssertOK()
            .AssertValue(r);
    }

    [Test]
    public void Create_WithShippingAddress()
    {
        // Arrange.
        var c = new Customer
        {
            FirstName = "Lisa",
            LastName = "Simpson",
            Email = "lisa.simpson@example.com",
            CustomerTypeCode = "IND",
            ContactMethodCode = "SMS",
            ShippingAddress = new Address { Street1 = "742 Evergreen Terrace", City = "Springfield", PostCode = "49007", State = "IL" }
        };

        // Act/Assert.
        var r = Test.Http<Customer>()
            .ExpectIdentifier()
            .ExpectETag()
            .ExpectChangeLogCreated()
            .ExpectCosmosDbOutboxEvents(e => e.AssertWithValue("contoso", "contoso.customers.customer.created.v1"))
            .Run(HttpMethod.Post, "/api/customers", c)
            .AssertCreated()
            .AssertLocationHeader(r => new Uri($"/api/customers/{r!.Id}", UriKind.Relative))
            .Value!;

        r.ShippingAddress.Should().NotBeNull();
        r.ShippingAddress!.City.Should().Be("Springfield");

        // Assert.
        Test.Http<Customer>()
            .Run(HttpMethod.Get, $"/api/customers/{r.Id}")
            .AssertOK()
            .AssertValue(r);
    }
}
