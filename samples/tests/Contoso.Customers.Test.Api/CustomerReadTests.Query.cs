namespace Contoso.Customers.Test.Api;

public partial class CustomerReadTests : WithApiTester<Contoso.Customers.Api.Program>
{
    [Test]
    public void Query_Schema()
    {
        Test.Http()
            .Run(HttpMethod.Get, "/api/customers/$query")
            .AssertOK()
            .GetContent().Should().BeJson()
                .ContainAll(["$.filter.fields.firstname", "$.filter.fields.lastname", "$.filter.fields.email", "$.filter.fields.customertype", "$.orderby.fields.lastname", "$.orderby.fields.firstname", "$.orderby.default"]);
    }

    [Test]
    public void Query_All()
    {
        // No $take specified - unlike the EF-backed domains (which default to PagingArgs.DefaultTake), CosmosDbQuery applies no paging at all when none is requested, so every seeded row comes back.
        var r = Test.Http<CustomerLite[]>()
            .Run(HttpMethod.Get, "/api/customers")
            .AssertOK()
            .Value;

        r.Should().NotBeNull().And.HaveCount(6);
    }

    [Test]
    public void Query_Paging()
    {
        // Default order (lastname asc): Anderson, Brown, Clarke, Davis, Edwards, Foster - skip 2, take 2 -> Clarke, Davis.
        var r = Test.Http<CustomerLite[]>()
            .Run(HttpMethod.Get, "/api/customers?$skip=2&$take=2&$count=true")
            .AssertOK();

        r.Value.Should().NotBeNull().And.HaveCount(2);
        r.Value!.Select(c => c.LastName).Should().ContainInOrder("Clarke", "Davis");

        r.Response.Headers.Should().ContainKey("X-Paging-Skip").WhoseValue.Should().ContainSingle().Which.Should().Be("2");
        r.Response.Headers.Should().ContainKey("X-Paging-Take").WhoseValue.Should().ContainSingle().Which.Should().Be("2");
        r.Response.Headers.Should().ContainKey("X-Paging-Total-Count").WhoseValue.Should().ContainSingle().Which.Should().Be("6");
    }

    [Test]
    public void Query_FilterByLastName_StartsWith()
    {
        var r = Test.Http<CustomerLite[]>()
            .Run(HttpMethod.Get, "/api/customers?$filter=startswith(lastname, 'And')")
            .AssertOK()
            .Value;

        r.Should().NotBeNull().And.HaveCount(1).And.OnlyContain(c => c.LastName == "Anderson");
    }

    [Test]
    public void Query_FilterByLastName_EqualityNotSupported()
    {
        // LastName only supports StringFunctions (startswith/contains/endswith) - no 'eq' - an unsupported operator is a 400, not a silently-ignored filter.
        Test.Http()
            .Run(HttpMethod.Get, "/api/customers?$filter=lastname eq 'Brown'")
            .AssertBadRequest();
    }

    [Test]
    public void Query_FilterByFirstName_Contains()
    {
        var r = Test.Http<CustomerLite[]>()
            .Run(HttpMethod.Get, "/api/customers?$filter=contains(firstname, 'lice')")
            .AssertOK()
            .Value;

        r.Should().NotBeNull().And.HaveCount(2)
            .And.OnlyContain(c => c.FirstName == "Alice")
            .And.BeInAscendingOrder(c => c.LastName);
    }

    [Test]
    public void Query_FilterByEmail_Eq()
    {
        var r = Test.Http<CustomerLite[]>()
            .Run(HttpMethod.Get, "/api/customers?$filter=email eq 'bob.brown@example.com'")
            .AssertOK()
            .Value;

        r.Should().NotBeNull().And.HaveCount(1).And.OnlyContain(c => c.LastName == "Brown");
    }

    [Test]
    public void Query_FilterByCustomerType_Eq()
    {
        var r = Test.Http<CustomerLite[]>()
            .Run(HttpMethod.Get, "/api/customers?$filter=customertype eq 'ORG'")
            .AssertOK()
            .Value;

        r.Should().NotBeNull().And.HaveCount(2).And.OnlyContain(c => c.CustomerTypeCode == "ORG");
    }

    [Test]
    public void Query_FilterByCustomerType_Invalid()
    {
        Test.Http()
            .Run(HttpMethod.Get, "/api/customers?$filter=customertype eq 'ZZ'")
            .AssertBadRequest();
    }

    [Test]
    public void Query_OrderBy_LastName_Desc()
    {
        var r = Test.Http<CustomerLite[]>()
            .Run(HttpMethod.Get, "/api/customers?$orderby=lastname desc")
            .AssertOK()
            .Value;

        r.Should().NotBeNull().And.HaveCount(6).And.BeInDescendingOrder(c => c.LastName);
    }

    [Test]
    public void Query_OrderBy_FirstName()
    {
        // LastName is configured WithAlwaysInclude(), so this is a two-property "ORDER BY firstName, lastName" under the hood - requires the composite index DatabaseSetUp configures on "customers".
        var r = Test.Http<CustomerLite[]>()
            .Run(HttpMethod.Get, "/api/customers?$orderby=firstname")
            .AssertOK()
            .Value;

        r.Should().NotBeNull().And.HaveCount(6);
        r!.Select(c => c.FirstName).Should().ContainInOrder("Alice", "Alice", "Bob", "Charlie", "Diana", "Frank");

        // Tie-break: the always-included LastName keeps its own (ascending) direction regardless of what FirstName's direction was requested as, so Anderson sorts before Edwards.
        r[0].LastName.Should().Be("Anderson");
        r[1].LastName.Should().Be("Edwards");
    }
}
