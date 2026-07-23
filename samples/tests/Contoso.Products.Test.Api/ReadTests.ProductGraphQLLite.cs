namespace Contoso.Products.Test.Api;

public partial class ReadTests : WithApiTester<Contoso.Products.Api.Program>
{
    [Test]
    public void GraphQLLite_Products_All()
    {
        var r = Test.Http<JsonElement>()
            .Run(HttpMethod.Post, "/api/products/query", new { query = "{ products { id sku } }" })
            .AssertOK()
            .Value;

        r.GetProperty("data").GetProperty("products").GetArrayLength().Should().Be(25);
    }

    [Test]
    public void GraphQLLite_Products_FilterOrderByPagingFieldSelection_MatchesRest()
    {
        // Act: GraphQL-lite bridge.
        var gql = Test.Http<JsonElement>()
            .Run(HttpMethod.Post, "/api/products/query", new
            {
                query = "query($filter: String, $orderby: String, $skip: Int, $take: Int) { products(filter: $filter, orderby: $orderby, skip: $skip, take: $take) { sku text } }",
                variables = new { filter = "startswith(Sku, 'spec')", orderby = "text desc", skip = 0, take = 10 }
            })
            .AssertOK()
            .Value;

        var gqlProducts = gql.GetProperty("data").GetProperty("products");

        // Act: existing REST $query endpoint, over the same underlying QueryAsync pipeline.
        var rest = Test.Http<ProductLite[]>()
            .Run(HttpMethod.Get, "/api/products?$filter=startswith(Sku, 'spec')&$orderby=text desc&$fields=sku,text")
            .AssertOK()
            .Value!;

        // Assert: same rows, same order, same (sku/text-only) field shape via both bridges.
        gqlProducts.GetArrayLength().Should().Be(rest.Length);

        for (var i = 0; i < rest.Length; i++)
        {
            var item = gqlProducts[i];
            item.GetProperty("sku").GetString().Should().Be(rest[i].Sku);
            item.GetProperty("text").GetString().Should().Be(rest[i].Text);
            item.TryGetProperty("id", out _).Should().BeFalse(); // Only requested fields are present - proves JsonFilter projection parity.
        }
    }

    [Test]
    public void GraphQLLite_Product_Get_NotFound()
    {
        var r = Test.Http<JsonElement>()
            .Run(HttpMethod.Post, "/api/products/query", new { query = "{ product(id: \"" + Guid.Empty + "\") { sku } }" })
            .AssertOK()
            .Value;

        r.TryGetProperty("errors", out var errors).Should().BeTrue();
        errors.GetArrayLength().Should().BeGreaterThan(0);
        errors[0].GetProperty("extensions").GetProperty("code").GetString().Should().Be("NOT_FOUND");
    }

    [Test]
    public void GraphQLLite_UnknownField_ReturnsError()
    {
        var r = Test.Http<JsonElement>()
            .Run(HttpMethod.Post, "/api/products/query", new { query = "{ products { id nope } }" })
            .AssertOK()
            .Value;

        r.TryGetProperty("errors", out var errors).Should().BeTrue();
        errors.GetArrayLength().Should().BeGreaterThan(0);
    }
}
