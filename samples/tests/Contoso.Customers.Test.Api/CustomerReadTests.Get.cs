namespace Contoso.Customers.Test.Api;

public partial class CustomerReadTests : WithApiTester<Contoso.Customers.Api.Program>
{
    [Test]
    public void Get_NotFound()
    {
        Test.Http()
            .Run(HttpMethod.Get, "/api/customers/404")
            .AssertNotFound();
    }

    [Test]
    public void Get_Found()
    {
        var id = 16.ToGuid().ToString();

        var c = Test.Http<Customer>()
            .Run(HttpMethod.Get, $"/api/customers/{id}")
            .AssertOK()
            .Value!;

        c.Id.Should().Be(id);
        c.FirstName.Should().Be("Frank");
        c.LastName.Should().Be("Foster");
        c.Email.Should().Be("frank.foster@example.com");
        c.Phone.Should().Be("555-0100");
        c.ShippingAddress.Should().NotBeNull();
        c.ShippingAddress!.Street1.Should().Be("1 Test Street");
        c.ShippingAddress.City.Should().Be("Springfield");
        c.ShippingAddress.PostCode.Should().Be("49007");
        c.ShippingAddress.State.Should().Be("IL");
        c.CustomerTypeCode.Should().Be("IND");
        c.ContactMethodCode.Should().Be("EM");
        c.HasShopped.Should().BeFalse();
        c.ETag.Should().NotBeNullOrEmpty();
        c.ChangeLog.Should().BeNull(); // Raw-seeded row (never went through Create/Update), so no changeLog was ever written.
    }

    [Test]
    public void Get_Not_Modified()
    {
        var id = 11.ToGuid().ToString();

        var r = Test.Http()
            .Run(HttpMethod.Get, $"/api/customers/{id}")
            .AssertOK()
            .Response;

        r.Headers.ETag.Should().NotBeNull();

        Test.Http()
            .Run(HttpMethod.Get, $"/api/customers/{id}", requestModifier: rm => rm.WithIfNoneMatch(r.Headers.ETag.Tag))
            .AssertNotModified();
    }
}
