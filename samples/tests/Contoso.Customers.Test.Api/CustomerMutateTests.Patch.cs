namespace Contoso.Customers.Test.Api;

public partial class CustomerMutateTests : WithApiTester<Contoso.Customers.Api.Program>
{
    [Test]
    public void Patch_NotFound()
    {
        // Act/Assert. No If-Match needed - the get returns null and short-circuits before the ETag comparison.
        Test.Http()
            .Run(HttpMethod.Patch, "/api/customers/404", new { lastName = "Updated" }, requestModifier: r => r.WithMergePatchJsonContentType())
            .AssertNotFound();
    }

    [Test]
    public void Patch_Concurrency()
    {
        // Arrange.
        var c = Test.Http<Customer>()
            .Run(HttpMethod.Get, $"/api/customers/{4.ToGuid()}")
            .AssertOK()
            .Value!;

        // Act/Assert.
        Test.Http()
            .Run(HttpMethod.Patch, $"/api/customers/{c.Id}", new { lastName = "Updated" }, requestModifier: r => r.WithIfMatch("AAAAAAAA").WithMergePatchJsonContentType())
            .AssertPreconditionFailed();
    }

    [Test]
    public void Patch_Validation()
    {
        // Arrange.
        var c = Test.Http<Customer>()
            .Run(HttpMethod.Get, $"/api/customers/{4.ToGuid()}")
            .AssertOK()
            .Value!;

        // Act/Assert.
        Test.Http()
            .Run(HttpMethod.Patch, $"/api/customers/{c.Id}", new { customerType = "XX" }, requestModifier: r => r.WithIfMatch(c.ETag).WithMergePatchJsonContentType())
            .AssertBadRequest()
            .AssertErrors("Customer type is invalid.");
    }

    [Test]
    public void Patch_Success()
    {
        // Arrange.
        var c = Test.Http<Customer>()
            .Run(HttpMethod.Get, $"/api/customers/{5.ToGuid()}")
            .AssertOK()
            .Value!;

        // Act/Assert.
        var u = Test.Http<Customer>()
            .ExpectCosmosDbOutboxEvents(e => e.AssertWithValue("contoso", "contoso.customers.customer.updated.v1"))
            .Run(HttpMethod.Patch, $"/api/customers/{c.Id}", new { lastName = "Patched" }, requestModifier: r => r.WithIfMatch(c.ETag).WithMergePatchJsonContentType())
            .AssertOK()
            .Value!;

        u.LastName.Should().Be("Patched");
        u.ETag.Should().NotBe(c.ETag);

        // Assert.
        Test.Http<Customer>()
            .Run(HttpMethod.Get, $"/api/customers/{c.Id}")
            .AssertOK()
            .AssertValue(u);
    }

    [Test]
    public void Patch_NoChanges()
    {
        // Arrange.
        var c = Test.Http<Customer>()
            .Run(HttpMethod.Get, $"/api/customers/{6.ToGuid()}")
            .AssertOK()
            .Value!;

        // Act/Assert. An empty merge patch has no changes - put() is never invoked, so no event is published and the ETag/ChangeLog stay untouched.
        var u = Test.Http<Customer>()
            .ExpectNoCosmosDbOutboxEvents()
            .Run(HttpMethod.Patch, $"/api/customers/{c.Id}", new { }, requestModifier: r => r.WithIfMatch(c.ETag).WithMergePatchJsonContentType())
            .AssertOK()
            .Value!;

        u.ETag.Should().Be(c.ETag);
        u.ChangeLog.Should().BeNull(); // Raw-seeded row (never went through Create/Update) - no changeLog was ever written, and this no-op patch must not add one either.

        // Assert.
        Test.Http<Customer>()
            .Run(HttpMethod.Get, $"/api/customers/{c.Id}")
            .AssertOK()
            .AssertValue(u);
    }
}
