namespace Contoso.Test.Aspire;

public partial class E2ETest
{
    [Test]
    public async Task ProductSearch_LifecycleIndexesThroughOutboxAsync()
    {
        var product = Test.Http<Product>(_productsApi)
            .Run(HttpMethod.Post, "/api/products", new Product
            {
                Sku = "SEARCH-E2E-CLEANER",
                Text = "Search lifecycle bike cleaner",
                Description = "Removes mud from bicycle frames.",
                Price = 12m,
                SubCategoryCode = "CL",
                UnitOfMeasureCode = "EA"
            }, r => r.WithIdempotencyKey())
            .AssertCreated().Value!;

        await using var source = Npgsql.NpgsqlDataSource.Create(_productsConnectionString);
        await WaitForDocumentAsync(source, product.Id!, product.Description).ConfigureAwait(false);
        SearchProduct(product.Sku!, includeInactive: false).Should().BeEmpty();
        SearchProduct(product.Sku!, includeInactive: true).Should().ContainSingle();

        product.Description = "A citrus degreaser for oily bicycle drivetrains.";
        product = Test.Http<Product>(_productsApi).Run(HttpMethod.Put, $"/api/products/{product.Id}", product).AssertOK().Value!;
        await WaitForDocumentAsync(source, product.Id!, product.Description).ConfigureAwait(false);

        // Delete while inactive: logical deletion must clean the derived table without a physical cascade.
        Test.Http(_productsApi).Run(HttpMethod.Delete, $"/api/products/{product.Id}").AssertNoContent();
        SearchProduct(product.Sku!, includeInactive: true).Should().BeEmpty();
        await WaitForDocumentAsync(source, product.Id!, null).ConfigureAwait(false);
        Test.Http(_productsApi).Run(HttpMethod.Get, $"/api/products/{product.Id}").AssertNotFound();

        var active = Test.Http<Product>(_productsApi)
            .Run(HttpMethod.Post, "/api/products", new Product
            {
                Sku = "SEARCH-E2E-ACTIVE",
                Text = "Search lifecycle trail bicycle",
                Description = "A bicycle for forest trails.",
                Price = 900m,
                SubCategoryCode = "XC",
                UnitOfMeasureCode = "EA"
            }, r => r.WithIdempotencyKey())
            .AssertCreated().Value!;
        Test.Http<Product>(_productsApi).Run(HttpMethod.Post, $"/api/products/{active.Id}/activate").AssertOK();
        await WaitForDocumentAsync(source, active.Id!, active.Description).ConfigureAwait(false);
        SearchProduct(active.Sku!, includeInactive: false).Should().ContainSingle().Which.IsInactive.Should().BeFalse();
    }

    private ProductLite[] SearchProduct(string sku, bool includeInactive) => Test.Http<ProductLite[]>(_productsApi)
        .Run(HttpMethod.Get, $"/api/products?$search=bicycle&$filter=sku eq '{sku}'{(includeInactive ? "&$inactive" : "")}")
        .AssertOK().Value!;

    private async Task WaitForDocumentAsync(Npgsql.NpgsqlDataSource source, string id, string? expected, CancellationToken cancellationToken = default)
    {
        for (var attempt = 0; attempt < 90; attempt++)
        {
            await using var command = source.CreateCommand("SELECT document FROM products.product_search WHERE product_id = $1 AND profile = $2 AND recipe = $3");
            command.Parameters.AddWithValue(id);
            command.Parameters.AddWithValue(OllamaHttpClient.Profile);
            command.Parameters.AddWithValue(Contoso.Products.Application.Search.ProductSearchDocument.TemplateVersion);
            var document = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) as string;
            if (expected is null ? document is null : document?.Contains(expected, StringComparison.Ordinal) == true)
                return;
            Test.Delay(1000, $"Waiting for product '{id}' search projection after outbox delivery; poll {attempt + 1}/90.");
        }
        Assert.Fail($"Search projection for product '{id}' did not {(expected is null ? "disappear" : "contain the current description")} after 90 polls.");
    }
}
