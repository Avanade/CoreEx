namespace Contoso.Products.Test.Api;

[TestFixture, Explicit("Builds and verifies the selected real search branch in the isolated contoso_search_eval database.")]
public class ProductSearchRelevanceSetupTests : WithApiTester<Contoso.Products.Api.Program>
{
    private int _expectedEligibleCount;

    [OneTimeSetUp]
    public async Task OneTimeSetUpAsync()
    {
        var connectionString = Environment.GetEnvironmentVariable("Aspire__Npgsql__ConnectionString");
        var database = string.IsNullOrWhiteSpace(connectionString) ? null : new Npgsql.NpgsqlConnectionStringBuilder(connectionString).Database;
        if (!string.Equals(database, "contoso_search_eval", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("This fixture only runs against the isolated contoso_search_eval database. Run the explicit DbEx Reset, Data, and Execute seed sequence first.");

        await Test.ClearFusionCacheAsync().ConfigureAwait(false);

        Test.Scoped(test => test.Run(async _ =>
        {
            var mode = Environment.GetEnvironmentVariable("Products__Search__Mode");
            var lexicalOnly = mode == "FullText";
            var hybrid = mode == "Hybrid";
            var database = test.Services.GetRequiredService<ProductsDbContext>().Database;
            _expectedEligibleCount = await database
                .SqlQuery<int>($"SELECT COUNT(*)::integer AS \"Value\" FROM products.product WHERE is_inactive = false AND is_deleted = false AND sku <> 'LABOR'")
                .SingleAsync().ConfigureAwait(false);
            var expectedIndexedCount = await database
                .SqlQuery<int>($"SELECT COUNT(*)::integer AS \"Value\" FROM products.product WHERE is_deleted = false")
                .SingleAsync().ConfigureAwait(false);
            var semanticIndex = test.Services.GetRequiredService<ProductSearchIndex>();
            var lexicalIndex = test.Services.GetRequiredService<ProductLexicalSearchIndex>();
            var count = lexicalOnly
                ? await lexicalIndex.RebuildAsync().ConfigureAwait(false)
                : await semanticIndex.RebuildAsync().ConfigureAwait(false);
            count.Should().Be(expectedIndexedCount);

            if (!lexicalOnly)
            {
                await semanticIndex.EnsureReadyAsync().ConfigureAwait(false);
                var profile = test.Services.GetRequiredService<IProductEmbeddingAdapter>().Profile;
                var recipe = ProductSearchDocument.TemplateVersion;
                var indexed = await test.Services.GetRequiredService<ProductsDbContext>().Database
                    .SqlQuery<int>($"SELECT COUNT(*)::integer AS \"Value\" FROM products.product_search WHERE profile = {profile} AND recipe = {recipe}")
                    .SingleAsync().ConfigureAwait(false);
                indexed.Should().Be(expectedIndexedCount);
            }

            if (lexicalOnly || hybrid)
            {
                await lexicalIndex.EnsureReadyAsync().ConfigureAwait(false);
                var indexed = await test.Services.GetRequiredService<ProductsDbContext>().Database
                    .SqlQuery<int>($"SELECT COUNT(*)::integer AS \"Value\" FROM products.product_lexical_search WHERE recipe = {ProductLexicalSearchIndex.Recipe}")
                    .SingleAsync().ConfigureAwait(false);
                indexed.Should().Be(expectedIndexedCount);
            }
        }).AssertSuccess());
    }

    [Test]
    public void Product_Search_RealModelProfileIsReady()
    {
        var mode = Environment.GetEnvironmentVariable("Products__Search__Mode");
        var lexical = mode == "FullText";
        var hybrid = mode == "Hybrid";
        var response = Test.Http<ProductLite[]>()
            .Run(HttpMethod.Get, lexical
                ? "/api/products?$search=SHIMANO-XT-M8100-RD&$take=1&$count=true"
                : hybrid
                ? "/api/products?$search=SHIMANO-XT-M8100-RD&$take=1&$count=true"
                : "/api/products?$search=something to clean mud off my bike&$filter=sku ne 'LABOR'&$take=1&$count=true")
            .AssertOK();

        if (lexical)
            response.Value.Should().ContainSingle().Which.Sku.Should().Be("SHIMANO-XT-M8100-RD");
        else if (hybrid)
        {
            response.Value.Should().ContainSingle().Which.Sku.Should().Be("SHIMANO-XT-M8100-RD");
            var candidateCount = int.Parse(response.Response.Headers.GetValues("X-Paging-Total-Count").Single());
            candidateCount.Should().BeLessThanOrEqualTo(ProductSearchFusion.MaximumCandidateCount);
        }
        else
            response.Response.Headers.Should().ContainKey("X-Paging-Total-Count")
                .WhoseValue.Should().ContainSingle().Which.Should().Be(_expectedEligibleCount.ToString());
    }
}
