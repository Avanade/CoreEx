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
            .ExpectLogContains("ORDER BY p.text DESC, p.sku")
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
            .Run(HttpMethod.Get, "/api/products?$filter=category eq 'M'")
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
            .Run(HttpMethod.Get, "/api/products?$filter=subcategory eq 'XC' and brand in ('YETI', 'CANYON')")
            .AssertOK()
            .Value;

        r.Should().NotBeNull().And.HaveCount(2);
    }

    [Test]
    public void Product_Query_Search_MultipleTerms()
    {
        var r = Test.Http<ProductLite[]>()
            .Run(HttpMethod.Get, "/api/products?$search=rear derailleur&$take=3")
            .AssertOK()
            .Value;

        r.Should().NotBeNull()
            .And.HaveCount(3)
            .And.OnlyContain(p => p.Text!.Contains("Rear Derailleur"));
    }

    [Test]
    public void Product_Query_Search_SkuIntent()
    {
        var r = Test.Http<ProductLite[]>()
            .Run(HttpMethod.Get, "/api/products?$search=sram-xx1&$take=1")
            .AssertOK()
            .Value;

        r.Should().NotBeNull()
            .And.ContainSingle()
            .Which.Sku.Should().Be("SRAM-XX1-EAGLE-CASSETTE");
    }

    [Test]
    public void Product_Query_Search_WithFilterAndOrderBy()
    {
        Test.Http()
            .Run(HttpMethod.Get, "/api/products?$search=cleaner&$filter=brand eq 'MUCOFF'&$orderby=text desc")
            .AssertBadRequest();
    }

    [Test]
    public void Product_Query_FilterByPriceRange()
    {
        var result = Test.Http<ProductLite[]>()
            .Run(HttpMethod.Get, "/api/products?$filter=price ge 99 and price lt 130&$count=true")
            .AssertOK();

        result.Value.Should().NotBeNull().And.HaveCount(2)
            .And.OnlyContain(p => p.Price >= 99 && p.Price < 130);
        result.Value!.Select(p => p.Sku).Should().BeEquivalentTo(["SHIMANO-SLX-M7100-CASSETTE", "SHIMANO-XT-M8100-RD"]);
        result.Response.Headers.Should().ContainKey("X-Paging-Total-Count").WhoseValue.Should().ContainSingle().Which.Should().Be("2");
    }

    [TestCase("price eq 16.99")]
    [TestCase("price in (16.99, 17.50)")]
    [TestCase("price gt 16.98 and price le 16.99")]
    [TestCase("brand eq 'MUCOFF' and price ne 8 and price ne 25")]
    public void Product_Query_FilterByDecimalPrice(string filter)
    {
        var products = Test.Http<ProductLite[]>()
            .Run(HttpMethod.Get, $"/api/products?$filter={Uri.EscapeDataString(filter)}")
            .AssertOK()
            .Value!;

        products.Should().ContainSingle().Which.Sku.Should().Be("MUCOFF-NANO-CLEANER");
    }

    [Test]
    public void Product_Query_Search_WithPriceFilter()
    {
        var products = Test.Http<ProductLite[]>()
            .Run(HttpMethod.Get, "/api/products?$search=cleaner&$filter=price lt 20&$take=1")
            .AssertOK()
            .Value!;

        products.Should().ContainSingle().Which.Sku.Should().Be("MUCOFF-NANO-CLEANER");
        products[0].Description.Should().NotBeNullOrEmpty();
    }

    [Test]
    public void Product_Query_Search_TooLong()
    {
        Test.Http()
            .Run(HttpMethod.Get, $"/api/products?$search={new string('a', 201)}")
            .AssertBadRequest()
            .AssertErrors(new ApiError("$search", "Statement must not exceed 200 characters in length."));
    }
}
