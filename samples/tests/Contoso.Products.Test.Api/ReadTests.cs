namespace Contoso.Products.Test.Api;

public partial class ReadTests : WithApiTester<Contoso.Products.Api.Program>
{
    private readonly TestEmbeddingAdapter _embeddings = new();

    [OneTimeSetUp]
    public async Task OneTimeSetUpAsync()
    {
        Test.ReplaceScoped<IProductEmbeddingAdapter>(_ => _embeddings);
        await Test.MigratePostgresDataAsync<TestData>(["read-data.seed.yaml"], DbMigration.ConfigureMigrationArgs).ConfigureAwait(false);
        await Test.ClearFusionCacheAsync().ConfigureAwait(false);
        RebuildSearch();
    }

    private void RebuildSearch() => Test.Scoped(test => test.Run(async _ => await test.Services.GetRequiredService<ProductSearchIndex>().RebuildAsync().ConfigureAwait(false)).AssertSuccess());
}
