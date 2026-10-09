namespace Contoso.Customers.Test.Api;

public partial class HostTests : WithApiTester<Contoso.Customers.Api.Program>
{
    [OneTimeSetUp]
    public async Task OneTimeSetUpAsync()
    {
        await Test.MigrateCosmosDataAsync<TestData>(configure: Contoso.Customers.Database.Program.ConfigureProvisionArgs).ConfigureAwait(false);
        await Test.ClearFusionCacheAsync().ConfigureAwait(false);
    }

    [Test]
    public void Swagger_UI()
    {
        // Hit swagger and assert redirect.
        Test.Http()
            .Run(HttpMethod.Get, "/swagger")
            .Assert(HttpStatusCode.Found)
            .AssertLocationHeader(new Uri("/swagger/index.html", UriKind.Relative));

        // Go to redirected URL and assert basic content.
        Test.Http()
            .Run(HttpMethod.Get, "/swagger/index.html")
            .Assert(HttpStatusCode.OK)
            .GetContent().Should().Contain("<title>Swagger UI</title>");
    }

    [Test]
    public void Swagger_Json()
    {
        Test.Http()
            .Run(HttpMethod.Get, "/swagger/v1/swagger.json")
            .Assert(HttpStatusCode.OK)
            .AssertContentTypeJson()
            .GetContent().Should().BeJson()
                .ContainAll(["$.openapi", "$.info", "$.paths"])
                .HavePath("$.info.title").GetValue<string>().Should().Be("Contoso.Customers.Api");
    }

    [TestCase("/health/live")]
    [TestCase("/health/startup")]
    [TestCase("/health/ready")]
    public void Health(string path)
    {
        Test.Http()
            .Run(HttpMethod.Get, path)
            .Response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable);
    }

    [TestCase("/health/live/detailed", true)]
    [TestCase("/health/startup/detailed", false)]
    [TestCase("/health/ready/detailed", false)]
    public void Health_Detailed(string path, bool minimal)
    {
        // Liveness reflects the process, not downstream dependencies.
        string[] paths = ["$.entries.reference-data-orchestrator", "$.entries.cosmos-database", "$.entries['stackExchange.Redis']"];

        var r = Test.Http()
            .Run(HttpMethod.Get, path)
            .AssertContentTypeJson();

        r.Response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = r.GetContent().Should().BeJson();
        if (minimal)
            json.NotContainAny(paths);
        else
            json.ContainAll(paths);
    }

    [Test]
    public void Redis_DistributedCache_RoundTrip()
    {
        Test.ScopedType<IHybridCache>(async test =>
        {
            var cache = test.Services.GetRequiredService<IHybridCache>();
            var key = $"redis-host-test-{Runtime.NewId()}";
            var options = new HybridCacheEntryOptions { Strategy = CacheStrategy.Distributed, DistributedExpiration = TimeSpan.FromMinutes(1) };
            try
            {
                await cache.SetByKeyAsync(key, "Customers Redis", options).ConfigureAwait(false);
                var (exists, value) = await cache.TryGetByKeyAsync<string>(key, options).ConfigureAwait(false);
                exists.Should().BeTrue();
                value.Should().Be("Customers Redis");
            }
            finally
            {
                await cache.RemoveByKeyAsync(key, options).ConfigureAwait(false);
            }
        });
    }

    [Test]
    public void Health_Ready_EmitsCosmosDependencySpan()
    {
        var activities = new ConcurrentQueue<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "Azure.Cosmos.Operation",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity => activities.Enqueue(activity)
        };
        ActivitySource.AddActivityListener(listener);

        Test.Http().Run(HttpMethod.Get, "/health/ready").AssertOK();
        activities.Should().Contain(a => a.DisplayName.StartsWith("read_database", StringComparison.Ordinal));
    }
}
