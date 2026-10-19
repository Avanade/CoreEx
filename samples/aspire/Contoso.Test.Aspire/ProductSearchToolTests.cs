namespace Contoso.Test.Aspire;

[TestFixture]
public class ProductSearchToolTests
{
    [Test]
    public async Task Search_WithoutFilters_PreservesSearchPagingAndRankOrder()
    {
        using var handler = new ProductsHandler("""[{"sku":"Z-FIRST","price":123.45},{"sku":"A-SECOND","price":20}]""");
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://products/") };
        var factory = new ProductsFactory(client);

        var result = await CommerceTools.SearchProductsAsync(factory, "mud & oil + #?/$='", take: 2);

        result.Select(x => x.Sku).Should().Equal("Z-FIRST", "A-SECOND");
        var query = Query(handler);
        query["$search"].ToString().Should().Be("mud & oil + #?/$='");
        query["$take"].ToString().Should().Be("2");
        query.Should().NotContainKey("$filter").And.NotContainKey("$orderby").And.NotContainKey("$inactive");
    }

    [Test]
    [SetCulture("fr-FR")]
    public async Task Search_AllFilters_AreConjoinedAndPricesAreCultureInvariant()
    {
        using var handler = new ProductsHandler("[]");
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://products/") };

        var result = await CommerceTools.SearchProductsAsync(new ProductsFactory(client), "descending with purpose", categoryCode: " b ",
            subCategoryCode: "em", brandCode: "yeti", minPrice: 1000.25m, maxPrice: 10000.50m);

        result.Should().BeEmpty();
        var filter = Query(handler)["$filter"].ToString();
        filter.Should().Be("category eq 'B' and subcategory eq 'EM' and brand eq 'YETI' and price ge 1000.25 and price le 10000.50");
    }

    [TestCase("category", "B", null, null, null, null, "category eq 'B'")]
    [TestCase("subcategory", null, "EM", null, null, null, "subcategory eq 'EM'")]
    [TestCase("brand", null, null, "FOX", null, null, "brand eq 'FOX'")]
    [TestCase("minimum", null, null, null, 0, null, "price ge 0")]
    [TestCase("maximum", null, null, null, null, 0, "price le 0")]
    [TestCase("equal bounds", null, null, null, 100, 100, "price ge 100 and price le 100")]
    public async Task Search_OptionalFilters(string scenario, string? categoryCode, string? subCategoryCode, string? brandCode, int? minPrice, int? maxPrice, string expected)
    {
        using var handler = new ProductsHandler("[]");
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://products/") };

        await CommerceTools.SearchProductsAsync(new ProductsFactory(client), scenario, categoryCode: categoryCode, subCategoryCode: subCategoryCode,
            brandCode: brandCode, minPrice: minPrice, maxPrice: maxPrice);

        Query(handler)["$filter"].ToString().Should().Be(expected);
    }

    [Test]
    public async Task Search_CodeLiteral_CannotIntroduceFilterOrQuerySyntax()
    {
        using var handler = new ProductsHandler("[]");
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://products/") };

        await CommerceTools.SearchProductsAsync(new ProductsFactory(client), "bike", brandCode: "YETI' or price ge 0 or brand eq 'FOX&$inactive=true");

        var query = Query(handler);
        query["$filter"].ToString().Should().Be("brand eq 'YETI'' OR PRICE GE 0 OR BRAND EQ ''FOX&$INACTIVE=TRUE'");
        query.Should().HaveCount(3).And.NotContainKey("$inactive");
    }

    [TestCase("", null, null)]
    [TestCase(null, " ", null)]
    [TestCase(null, null, "")]
    public void Search_BlankCode_IsRejectedBeforeHttp(string? categoryCode, string? subCategoryCode, string? brandCode)
    {
        using var handler = new ProductsHandler("[]");
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://products/") };

        Action act = () => CommerceTools.SearchProductsAsync(new ProductsFactory(client), "bike", categoryCode: categoryCode, subCategoryCode: subCategoryCode, brandCode: brandCode);

        act.Should().Throw<CoreEx.ValidationException>();
        handler.RequestUri.Should().BeNull();
    }

    [TestCase(-1, null)]
    [TestCase(null, -1)]
    [TestCase(100, 99)]
    public void Search_InvalidPrices_AreRejectedBeforeHttp(int? minimum, int? maximum)
    {
        using var handler = new ProductsHandler("[]");
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://products/") };

        Action act = () => CommerceTools.SearchProductsAsync(new ProductsFactory(client), "bike", minPrice: minimum, maxPrice: maximum);

        act.Should().Throw<ArgumentException>();
        handler.RequestUri.Should().BeNull();
    }

    [TestCase(0)]
    [TestCase(26)]
    public void Search_InvalidTake_IsRejectedBeforeHttp(int take)
    {
        using var handler = new ProductsHandler("[]");
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://products/") };
        Action act = () => CommerceTools.SearchProductsAsync(new ProductsFactory(client), "bike", take);
        act.Should().Throw<ArgumentOutOfRangeException>();
        handler.RequestUri.Should().BeNull();
    }

    [TestCase(HttpStatusCode.BadRequest)]
    [TestCase(HttpStatusCode.ServiceUnavailable)]
    public async Task Search_ApiFailure_IsNotAnEmptySuccess(HttpStatusCode status)
    {
        using var handler = new ProductsHandler("""{"title":"Search failed","status":400}""", status);
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://products/") };
        Func<Task> act = () => CommerceTools.SearchProductsAsync(new ProductsFactory(client), "bike", categoryCode: "UNKNOWN");
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Test]
    public async Task Options_ReadsLiveCodesAndParentCategories()
    {
        using var handler = new ProductsHandler("""{"categories":[{"code":"B","text":"Bikes"}],"sub-categories":[{"code":"EM","text":"E-MTB","categoryCode":"B"}],"brands":[{"code":"YETI","text":"Yeti Cycles"}]}""");
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://products/") };

        var options = await CommerceTools.GetProductSearchOptionsAsync(new ProductsFactory(client));

        handler.RequestUri.Should().Be(new Uri("http://products/api/refdata?name=categories&name=sub-categories&name=brands"));
        options.Categories.Should().ContainSingle().Which.Code.Should().Be("B");
        options.SubCategories.Should().ContainSingle().Which.Code.Should().Be("EM");
        options.Brands.Should().ContainSingle().Which.Code.Should().Be("YETI");
    }

    [Test]
    public void Search_ProtocolSchema_ExposesOptionalFiltersButNotServiceOrCancellation()
    {
        var tool = ModelContextProtocol.Server.McpServerTool.Create(CommerceTools.SearchProductsAsync);
        var schema = tool.ProtocolTool.InputSchema;
        var properties = schema.GetProperty("properties");
        foreach (var name in new[] { "categoryCode", "subCategoryCode", "brandCode", "minPrice", "maxPrice", "take" })
            properties.TryGetProperty(name, out _).Should().BeTrue();

        schema.GetProperty("required").EnumerateArray().Select(x => x.GetString()).Should().Contain("searchTerm");
        properties.TryGetProperty("cancellationToken", out _).Should().BeFalse();
    }

    private static Dictionary<string, Microsoft.Extensions.Primitives.StringValues> Query(ProductsHandler handler)
    {
        handler.RequestUri.Should().NotBeNull();
        handler.RequestUri!.AbsolutePath.Should().Be("/api/products");
        return Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(handler.RequestUri.Query);
    }

    private sealed class ProductsFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
        {
            name.Should().Be("ProductsApi");
            return client;
        }
    }

    private sealed class ProductsHandler(string body, HttpStatusCode status = HttpStatusCode.OK) : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            RequestUri = request.RequestUri;
            return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json") });
        }
    }
}
