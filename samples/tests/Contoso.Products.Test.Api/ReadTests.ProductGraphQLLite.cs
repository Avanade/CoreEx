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
                query = "query($where: ProductLiteWhereInput, $orderBy: [ProductLiteOrderByInput!], $first: Int) { products(where: $where, orderBy: $orderBy, first: $first) { edges { node { sku text } } } }",
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

    // The exact document a standard GraphQL client-tooling introspection handshake sends (e.g. Postman's/Insomnia's/GraphiQL's "fetch schema" action, or Apollo's
    // IntrospectionQuery) - see graphql-js's getIntrospectionQuery() with default options. Uses fragment spreads, which GraphQL-lite otherwise rejects when they appear
    // in an executable field's own selection set; __schema/__type are a deliberate exception (see GraphQLEngine), so this proves client schema-discovery genuinely works
    // end-to-end over HTTP, rather than only over the engine's unit-tested internals.
    [Test]
    public void GraphQLLite_Introspection_StandardClientQuery_Succeeds()
    {
        var query = Resource.GetString("ReadTests.ProductGraphQLLite_StandardIntrospectionQuery.graphql");

        var r = Test.Http<JsonElement>()
            .Run(HttpMethod.Post, "/api/query", new { query, operationName = "IntrospectionQuery" })
            .AssertOK()
            .Value;

        r.TryGetProperty("errors", out _).Should().BeFalse("a standard client introspection handshake must not fail");

        var schema = r.GetProperty("data").GetProperty("__schema");
        schema.GetProperty("queryType").GetProperty("name").GetString().Should().Be("Query");
        schema.GetProperty("mutationType").ValueKind.Should().Be(JsonValueKind.Null);

        var typeNames = schema.GetProperty("types").EnumerateArray().Select(t => t.GetProperty("name").GetString()).ToArray();
        typeNames.Should().Contain(["Query", "ProductLite", "ProductLiteConnection", "ProductLiteEdge", "ProductLiteWhereInput", "ProductLiteOrderByInput", "PageInfo"]);
    }

    [Test]
    public void GraphQLLite_Introspection_TypeByName_ReturnsFieldShape()
    {
        var r = Test.Http<JsonElement>()
            .Run(HttpMethod.Post, "/api/query", new { query = "{ __type(name: \"ProductLite\") { name kind fields { name } } }" })
            .AssertOK()
            .Value;

        var type = r.GetProperty("data").GetProperty("__type");
        type.GetProperty("name").GetString().Should().Be("ProductLite");
        type.GetProperty("kind").GetString().Should().Be("OBJECT");
        type.GetProperty("fields").EnumerateArray().Select(f => f.GetProperty("name").GetString()).Should().Contain("sku");
    }
}
