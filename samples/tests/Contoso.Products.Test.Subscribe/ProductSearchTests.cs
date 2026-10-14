namespace Contoso.Products.Test.Subscribe;

public class ProductSearchTests : WithApiTester<Contoso.Products.Subscribe.Program>
{
    private readonly ControlledEmbeddings _embeddings = new();
    private readonly string _id = 29.ToGuid().ToString();
    private string? _concurrentSql;

    [OneTimeSetUp]
    public void OneTimeSetUp() => Test.ReplaceScoped<IProductEmbeddingAdapter>(_ => _embeddings);

    [SetUp]
    public async Task SetUpAsync()
    {
        await Test.MigratePostgresDataAsync<TestData>(["mutate-data.seed.yaml"], DbMigration.ConfigureMigrationArgs).ConfigureAwait(false);
        await Test.ClearFusionCacheAsync().ConfigureAwait(false);
        _embeddings.Calls = 0;
        _embeddings.BeforeEmbedding = null;
        _embeddings.Fail = false;
        _concurrentSql = null;
        ExecuteSql("DELETE FROM products.product_search WHERE profile = 'tests:subscriber:768'");
        ExecuteSql("DELETE FROM products.product_search_readiness WHERE profile = 'tests:subscriber:768'");
    }

    [TestCase("created")]
    [TestCase("updated")]
    [TestCase("activated")]
    [TestCase("deactivated")]
    [TestCase("deleted")]
    public void ProductLifecycle_GlobReconcilesCurrentStateInsteadOfPayload(string action)
    {
        Receive(action);
        Count().Should().Be(1);
        _embeddings.Calls.Should().Be(1);
        Receive(action);
        _embeddings.Calls.Should().Be(1);
        Test.Scoped(test => test.Run(async _ =>
        {
            var index = test.Services.GetRequiredService<ProductSearchIndex>();
            Func<Task> ready = () => index.EnsureReadyAsync();
            await ready.Should().ThrowAsync<TransientException>().ConfigureAwait(false);
        }).AssertSuccess());
    }

    [Test]
    public void Update_ReembedsChangedContentButNotPriceOrActivity()
    {
        Receive("created");
        ExecuteSql("UPDATE products.product SET price = 19, is_inactive = true WHERE sku = 'MUCOFF-NANO-CLEANER'");
        Receive("updated");
        Receive("deactivated");
        _embeddings.Calls.Should().Be(1);
        ExecuteSql("UPDATE products.product SET description = 'Changed search text.' WHERE sku = 'MUCOFF-NANO-CLEANER'");
        Receive("updated");
        _embeddings.Calls.Should().Be(2);
    }

    [Test]
    public void LogicalDelete_RemovesEveryProfileAndOldUpdateCannotResurrect()
    {
        Receive("created");
        ExecuteSql("INSERT INTO products.product_search SELECT product_id, 'tests:old-profile', recipe, source_hash, document, embedding, indexed_on FROM products.product_search WHERE profile = 'tests:subscriber:768'");
        ExecuteSql("UPDATE products.product SET is_deleted = true WHERE sku = 'MUCOFF-NANO-CLEANER'");
        Receive("deleted");
        Count(allProfiles: true).Should().Be(0);
        Receive("updated");
        Count(allProfiles: true).Should().Be(0);
        _embeddings.Calls.Should().Be(1);
    }

    [Test]
    public void DeleteDuringInference_DoesNotPublishVector()
    {
        _concurrentSql = "UPDATE products.product SET is_deleted = true WHERE sku = 'MUCOFF-NANO-CLEANER'";
        Receive("created");
        Count(allProfiles: true).Should().Be(0);
    }

    [Test]
    public void EditDuringInference_RetriesWithoutPublishingObsoleteVector()
    {
        _concurrentSql = "UPDATE products.product SET description = 'Changed during inference.' WHERE sku = 'MUCOFF-NANO-CLEANER'";
        Receive("created", retry: true);
        Count().Should().Be(0);
        _concurrentSql = null;
        Receive("updated");
        Count().Should().Be(1);
    }

    [Test]
    public void ProviderFailure_RetriesAndPreservesPublishedVector()
    {
        Receive("created");
        ExecuteSql("UPDATE products.product SET description = 'Changed before provider failure.' WHERE sku = 'MUCOFF-NANO-CLEANER'");
        _embeddings.Fail = true;
        Receive("updated", retry: true);
        Count().Should().Be(1);
        _embeddings.Fail = false;
        Receive("updated");
        Count().Should().Be(1);
        _embeddings.Calls.Should().Be(3);
    }

    private void Receive(string action, bool retry = false) => Test.Scoped(test => test.Run(async _ =>
    {
        _embeddings.BeforeEmbedding = () =>
        {
            if (_concurrentSql is null)
                return;
            using var scope = test.Services.CreateScope();
            scope.ServiceProvider.GetRequiredService<ProductsDbContext>().Database.ExecuteSqlRaw(_concurrentSql);
        };
        var message = Test.CreateCloudEventFrom(EventData.CreateEvent<Product>(action).WithKey(_id)).ToServiceBusReceivedMessage();
        var result = await test.Services.GetRequiredKeyedService<ServiceBusSubscribedSubscriber>("subscriber-events").ReceiveAsync(message).ConfigureAwait(false);
        if (retry)
            result.Error.Should().BeOfType<EventSubscriberRetryException>(because: result.Error?.ToString()).Which.InnerException.Should().BeOfType<TransientException>();
        else
            result.IsSuccess.Should().BeTrue();
    }).AssertSuccess());

    private int Count(bool allProfiles = false)
    {
        var count = 0;
        Test.Scoped(test => test.Run(async _ =>
        {
            var db = test.Services.GetRequiredService<ProductsDbContext>().Database;
            count = allProfiles
                ? await db.SqlQuery<int>($"SELECT count(*)::int AS \"Value\" FROM products.product_search WHERE product_id = {_id}").SingleAsync().ConfigureAwait(false)
                : await db.SqlQuery<int>($"SELECT count(*)::int AS \"Value\" FROM products.product_search WHERE product_id = {_id} AND profile = 'tests:subscriber:768'").SingleAsync().ConfigureAwait(false);
        }).AssertSuccess());
        return count;
    }

    private void ExecuteSql(string sql) => Test.Scoped(test => test.Run(async _ =>
        await test.Services.GetRequiredService<ProductsDbContext>().Database.ExecuteSqlRawAsync(sql).ConfigureAwait(false)).AssertSuccess());

    private sealed class ControlledEmbeddings : IProductEmbeddingAdapter
    {
        public string Profile => "tests:subscriber:768";
        public int Dimensions => 768;
        public int Calls { get; set; }
        public bool Fail { get; set; }
        public Action? BeforeEmbedding { get; set; }
        public Task<Result<float[]>> EmbedDocumentAsync(string text, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Calls++;
            BeforeEmbedding?.Invoke();
            return Task.FromResult(Fail ? Result<float[]>.Fail(new HttpRequestException("Controlled provider failure.")) : Result.Ok(Enumerable.Repeat(0.1f, 768).ToArray()));
        }
        public Task<Result<float[]>> EmbedQueryAsync(string text, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
