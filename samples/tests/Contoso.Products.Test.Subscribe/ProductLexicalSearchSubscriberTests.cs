namespace Contoso.Products.Test.Subscribe;

[NonParallelizable]
public class ProductLexicalSearchSubscriberTests : WithApiTester<Contoso.Products.Subscribe.Program>
{
    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        Test.UseAdditionalConfiguration("Products:Search:Mode", "FullText");
        Test.ReplaceScoped<IProductEmbeddingAdapter>(_ => new UnavailableEmbeddings());
    }

    [SetUp]
    public async Task SetUpAsync()
    {
        await Test.MigratePostgresDataAsync<TestData>(["mutate-data.seed.yaml"], DbMigration.ConfigureMigrationArgs).ConfigureAwait(false);
        await Test.ClearFusionCacheAsync().ConfigureAwait(false);
    }

    [TestCase("created")]
    [TestCase("updated")]
    [TestCase("activated")]
    [TestCase("deactivated")]
    public void Event_ReconcilesWithoutModelAndIsIdempotent(string action)
    {
        Receive(action);
        Receive(action);
        AssertMatches(1);
    }

    [Test]
    public void DeleteAndLateUpdate_CannotResurrect()
    {
        Receive("created");
        Test.Scoped(test => test.Run(async _ =>
            await test.Services.GetRequiredService<ProductsDbContext>().Database.ExecuteSqlRawAsync(
                "UPDATE products.product SET is_deleted = true WHERE sku = 'MUCOFF-NANO-CLEANER'").ConfigureAwait(false)).AssertSuccess());
        Receive("deleted");
        Receive("updated");
        AssertMatches(0);
    }

    private void Receive(string action) => Test.Scoped(test => test.Run(async _ =>
    {
        var message = Test.CreateCloudEventFrom(EventData.CreateEvent<Product>(action).WithKey(29.ToGuid().ToString())).ToServiceBusReceivedMessage();
        (await test.Services.GetRequiredKeyedService<ServiceBusSubscribedSubscriber>("subscriber-events").ReceiveAsync(message).ConfigureAwait(false)).IsSuccess.Should().BeTrue();
    }).AssertSuccess());

    private void AssertMatches(int count) => Test.Scoped(test => test.Run(async _ =>
    {
        (await test.Services.GetRequiredService<ProductLexicalSearchIndex>().Matches("MUCOFF-NANO-CLEANER").CountAsync().ConfigureAwait(false)).Should().Be(count);
    }).AssertSuccess());

    private sealed class UnavailableEmbeddings : IProductEmbeddingAdapter
    {
        public string Profile => throw new InvalidOperationException("Full-text must not inspect the model profile.");
        public int Dimensions => throw new InvalidOperationException("Full-text must not inspect model dimensions.");
        public Task<Result<float[]>> EmbedDocumentAsync(string text, CancellationToken cancellationToken = default) => throw new HttpRequestException("Model is offline.");
        public Task<Result<float[]>> EmbedQueryAsync(string text, CancellationToken cancellationToken = default) => throw new HttpRequestException("Model is offline.");
    }
}
