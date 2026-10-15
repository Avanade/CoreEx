namespace Contoso.Products.Test.Api;

[TestFixture, Explicit("Builds and verifies the real Ollama search profile in the isolated contoso_search_eval database.")]
public class ProductSearchRelevanceSetupTests : WithApiTester<Contoso.Products.Api.Program>
{
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
            var count = await test.Services.GetRequiredService<ProductSearchIndex>().RebuildAsync().ConfigureAwait(false);
            count.Should().Be(31);
        }).AssertSuccess());
    }

    [Test]
    public void Product_Search_RealModelProfileIsReady()
    {
        var response = Test.Http<ProductLite[]>()
            .Run(HttpMethod.Get, "/api/products?$search=something to clean mud off my bike&$take=1&$count=true")
            .AssertOK();

        response.Response.Headers.Should().ContainKey("X-Paging-Total-Count")
            .WhoseValue.Should().ContainSingle().Which.Should().Be("29");
    }
}
