namespace Contoso.Products.Test.Api;

public partial class ReadTests : WithApiTester<Contoso.Products.Api.Program>
{
    [Test]
    public void Product_Query_Schema()
    {
        Test.Http<JsonElement>()
            .Run(HttpMethod.Get, "/api/products/$query")
            .AssertOK()
            .AssertJsonFromResource("ReadTests.Product_Query_Schema.res.json");
    }

    [Test]
    public void Product_Query_All()
    {
        var r = Test.Http<ProductLite[]>()
            .Run(HttpMethod.Get, "/api/products")
            .AssertOK()
            .Value;

        r.Should().NotBeNull().And.HaveCount(25);
        r.Single(x => x.Id == 1.ToGuid().ToString()).QtyOnHand.Should().Be(3);
    }

    [Test]
    public void Product_Query_Paging()
    {
        var r = Test.Http<ProductLite[]>()
            .Run(HttpMethod.Get, "/api/products?$skip=5&$take=10&$count=true")
            .AssertOK();

        r.Value.Should().NotBeNull().And.HaveCount(10);
        r.Value.Should().HaveCount(10);

        r.Response.Headers.Should().ContainKey("X-Paging-Skip").WhoseValue.Should().ContainSingle().Which.Should().Be("5");
        r.Response.Headers.Should().ContainKey("X-Paging-Take").WhoseValue.Should().ContainSingle().Which.Should().Be("10");
        r.Response.Headers.Should().ContainKey("X-Paging-Total-Count").WhoseValue.Should().ContainSingle().Which.Should().Be("29");
    }

    [Test]
    public void Product_Query_FilterBySku()
    {
        var r = Test.Http<ProductLite[]>()
            .Run(HttpMethod.Get, "/api/products?$filter=startswith(Sku, 'spec')")
            .AssertOK()
            .Value;

        r.Should().NotBeNull()
            .And.HaveCount(4)
            .And.OnlyContain(p => p.Sku!.StartsWith("SPEC")).And.BeInAscendingOrder(x => x.Sku).And.NotContain(x => x.Sku == "SPEC-EPIC-8-PRO");
    }

    [Test]
    public void Product_Query_FilterBySku_IncludeFields()
    {
        Test.Http<ProductLite[]>()
            .ExpectLogContains("ORDER BY [p].[Text] DESC, [p].[Sku]")
            .Run(HttpMethod.Get, "/api/products?$filter=startswith(Sku, 'spec')&$fields=sku,text&$orderby=text desc")
            .AssertOK()
            .AssertJsonFromResource("ReadTests.Product_Query_FilterBySku_IncludeFields.res.json");
    }

    [Test]
    public void Product_Query_FilterBySku_IncludeInactive()
    {
        var r = Test.Http<ProductLite[]>()
            .Run(HttpMethod.Get, "/api/products?$filter=startswith(Sku, 'spec')&$inactive")
            .AssertOK()
            .Value;

        r.Should().NotBeNull()
            .And.HaveCount(5)
            .And.OnlyContain(p => p.Sku!.StartsWith("SPEC")).And.BeInAscendingOrder(x => x.Sku).And.Contain(x => x.Sku == "SPEC-EPIC-8-PRO");
    }

    [Test]
    public void Product_Query_FilterByCategory()
    {
        var r = Test.Http<ProductLite[]>()
            .Run(HttpMethod.Get, "/api/products?$filter=category eq 'm'")
            .AssertOK()
            .Value;

        r.Should().NotBeNull()
            .And.HaveCount(1)
            .And.OnlyContain(p => p.Sku == "LABOR");
    }

    [Test]
    public void Product_Query_FilterByBrandAndSubCategory()
    {
        var r = Test.Http<ProductLite[]>()
            .Run(HttpMethod.Get, "/api/products?$filter=subcategory eq 'xc' and brand in ('yeti', 'canyon')")
            .AssertOK()
            .Value;

        r.Should().NotBeNull().And.HaveCount(2);
    }
}