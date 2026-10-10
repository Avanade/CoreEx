namespace Contoso.Shopping.Test.Api;

public partial class ReadTests
{
    private static string BasketsUrl(int customer, string? query = null) => $"/api/customers/{customer.ToGuid()}/baskets" + (query is null ? null : $"?{query}");

    private static string[] BasketIds(Basket[]? baskets) => [.. baskets!.Select(b => b.Id!)];

    [Test]
    public void Basket_Query_CustomerScoped()
    {
        var r = Test.Http<Basket[]>().Run(HttpMethod.Get, BasketsUrl(11)).AssertOK().Value;
        BasketIds(r).Should().BeEquivalentTo([3001.ToGuid().ToString(), 3003.ToGuid().ToString()]);
        r!.Should().OnlyContain(b => b.CustomerId == 11.ToGuid().ToString());
    }

    [Test]
    public void Basket_Query_UnknownCustomer()
    {
        var r = Test.Http<Basket[]>().Run(HttpMethod.Get, BasketsUrl(404)).AssertOK().Value;
        r.Should().NotBeNull().And.BeEmpty();
    }

    [Test]
    public void Basket_Query_Summary_ExcludesItemsAndShippingAddress()
    {
        var r = Test.Http<Basket[]>().Run(HttpMethod.Get, BasketsUrl(12)).AssertOK().Value;
        r.Should().ContainSingle();
        r![0].Id.Should().Be(3002.ToGuid().ToString());
        r[0].Items.Should().BeNull();
        r[0].ShippingAddress.Should().BeNull();
        r[0].Pricing!.Total.Should().Be(9533.81m);
    }

    [Test]
    public void Basket_Query_Filter_Status()
    {
        var r = Test.Http<Basket[]>().Run(HttpMethod.Get, BasketsUrl(13, "$filter=status eq 'B'")).AssertOK().Value;
        BasketIds(r).Should().Equal(3004.ToGuid().ToString());
    }

    [Test]
    public void Basket_Query_Filter_Total()
    {
        var r = Test.Http<Basket[]>().Run(HttpMethod.Get, BasketsUrl(11, "$filter=pricing.total gt 100")).AssertOK().Value;
        BasketIds(r).Should().Equal(3003.ToGuid().ToString());
    }

    [Test]
    public void Basket_Query_Filter_NeverCrossesCustomer()
    {
        // Basket 3002 belongs to customer 12; filtering on its status for customer 11 must not return it.
        var r = Test.Http<Basket[]>().Run(HttpMethod.Get, BasketsUrl(11, "$filter=status eq 'C'")).AssertOK().Value;
        r.Should().BeEmpty();
    }

    [Test]
    public void Basket_Query_OrderBy_Total()
    {
        var asc = Test.Http<Basket[]>().Run(HttpMethod.Get, BasketsUrl(11, "$orderby=pricing.total")).AssertOK().Value;
        BasketIds(asc).Should().Equal(3001.ToGuid().ToString(), 3003.ToGuid().ToString());

        var desc = Test.Http<Basket[]>().Run(HttpMethod.Get, BasketsUrl(11, "$orderby=pricing.total desc")).AssertOK().Value;
        BasketIds(desc).Should().Equal(3003.ToGuid().ToString(), 3001.ToGuid().ToString());
    }

    [Test]
    public void Basket_Query_OrderBy_Status()
    {
        var asc = Test.Http<Basket[]>().Run(HttpMethod.Get, BasketsUrl(11, "$orderby=status")).AssertOK().Value;
        BasketIds(asc).Should().Equal(3003.ToGuid().ToString(), 3001.ToGuid().ToString()); // A, E.

        var desc = Test.Http<Basket[]>().Run(HttpMethod.Get, BasketsUrl(11, "$orderby=status desc")).AssertOK().Value;
        BasketIds(desc).Should().Equal(3001.ToGuid().ToString(), 3003.ToGuid().ToString());
    }

    [Test]
    public void Basket_Query_OrderBy_CreatedOn_FinalTieBreak()
    {
        // All of customer 13's baskets have the same total (98); CreatedOn must deterministically sequence them, and repeated calls must agree.
        var r1 = Test.Http<Basket[]>().Run(HttpMethod.Get, BasketsUrl(13, "$orderby=pricing.total")).AssertOK().Value;
        var r2 = Test.Http<Basket[]>().Run(HttpMethod.Get, BasketsUrl(13, "$orderby=pricing.total")).AssertOK().Value;
        r1.Should().HaveCount(5);
        BasketIds(r1).Should().Equal(BasketIds(r2));
        r1!.Select(b => b.ChangeLog!.CreatedOn).Should().BeInAscendingOrder();
    }

    [Test]
    public void Basket_Query_Paging()
    {
        var r = Test.Http<Basket[]>().Run(HttpMethod.Get, BasketsUrl(13, "$orderby=pricing.total&$take=2&$skip=1")).AssertOK().Value;
        r.Should().HaveCount(2);
    }

    [Test]
    public void Basket_Query_InvalidField()
    {
        Test.Http().Run(HttpMethod.Get, BasketsUrl(11, "$filter=unknown eq 'X'")).AssertBadRequest();
        Test.Http().Run(HttpMethod.Get, BasketsUrl(11, "$orderby=unknown")).AssertBadRequest();
    }

    [Test]
    public void Basket_Query_Schema()
    {
        var r = Test.Http().Run(HttpMethod.Get, $"/api/customers/{11.ToGuid()}/baskets/$query").AssertOK().Response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        r.Should().Contain("pricing.total").And.Contain("status").And.Contain("createdon");
    }
}
