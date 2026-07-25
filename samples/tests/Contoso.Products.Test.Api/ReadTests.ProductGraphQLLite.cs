namespace Contoso.Products.Test.Api;

public partial class ReadTests : WithApiTester<Contoso.Products.Api.Program>
{
    [Test]
    public void GraphQLLite_Products_All()
    {
        var r = Test.Http<JsonElement>()
            .Run(HttpMethod.Post, "/api/query", new { query = "{ products { edges { node { id sku } } } }" })
            .AssertOK()
            .Value;

        r.GetProperty("data").GetProperty("products").GetProperty("edges").GetArrayLength().Should().Be(25);
    }

    [Test]
    public void GraphQLLite_Products_WhereOrderByPagingFieldSelection_MatchesRest()
    {
        // Act: GraphQL-lite bridge - native 'where'/'orderBy' syntax translated 1:1 to the same underlying QueryArgsConfig-driven query.
        var gql = Test.Http<JsonElement>()
            .Run(HttpMethod.Post, "/api/query", new
            {
                query = "query($where: ProductWhereInput, $orderBy: [ProductOrderByInput!], $first: Int) { products(where: $where, orderBy: $orderBy, first: $first) { edges { node { sku text } } } }",
                variables = new { where = new { sku = new { startsWith = "spec" } }, orderBy = new[] { new { text = "DESC" } }, first = 10 }
            })
            .AssertOK()
            .Value;

        var gqlProducts = gql.GetProperty("data").GetProperty("products").GetProperty("edges");

        // Act: existing REST $query endpoint, over the same underlying QueryAsync pipeline.
        var rest = Test.Http<ProductLite[]>()
            .Run(HttpMethod.Get, "/api/products?$filter=startswith(Sku, 'spec')&$orderby=text desc&$fields=sku,text")
            .AssertOK()
            .Value!;

        // Assert: same rows, same order, same (sku/text-only) field shape via both bridges.
        gqlProducts.GetArrayLength().Should().Be(rest.Length);

        for (var i = 0; i < rest.Length; i++)
        {
            var item = gqlProducts[i].GetProperty("node");
            item.GetProperty("sku").GetString().Should().Be(rest[i].Sku);
            item.GetProperty("text").GetString().Should().Be(rest[i].Text);
            item.TryGetProperty("id", out _).Should().BeFalse(); // Only requested fields are present - proves JsonFilter projection parity.
        }
    }

    [Test]
    public void GraphQLLite_Product_Get_Success()
    {
        var r = Test.Http<JsonElement>()
            .Run(HttpMethod.Post, "/api/query", new { query = "{ product(id: \"" + 1.ToGuid() + "\") { sku text } }" })
            .AssertOK()
            .Value;

        var product = r.GetProperty("data").GetProperty("product");
        product.GetProperty("sku").GetString().Should().Be("YETI-ASR-C2-2025");
        product.GetProperty("text").GetString().Should().Be("Yeti ASR C2");
    }

    [Test]
    public void GraphQLLite_Product_Get_NotFound()
    {
        var r = Test.Http<JsonElement>()
            .Run(HttpMethod.Post, "/api/query", new { query = "{ product(id: \"" + Guid.Empty + "\") { sku } }" })
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
            .Run(HttpMethod.Post, "/api/query", new { query = "{ products { edges { node { id nope } } } }" })
            .AssertOK()
            .Value;

        r.TryGetProperty("errors", out var errors).Should().BeTrue();
        errors.GetArrayLength().Should().BeGreaterThan(0);
    }
}
