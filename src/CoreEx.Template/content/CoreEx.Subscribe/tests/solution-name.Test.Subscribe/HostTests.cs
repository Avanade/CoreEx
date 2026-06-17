namespace solution-name.Test.Subscribe;

public partial class HostTests : WithApiTester<solution-name.Relay.Subscribe>
{
    [OneTimeSetUp]
    public async Task OneTimeSetUpAsync()
    {
// #if implement-sqlserver
        await Test.MigrateSqlServerDataAsync<TestData>(["no-data.seed.yaml"], DbMigration.ConfigureMigrationArgs).ConfigureAwait(false);
// #elif implement-postgres
        await Test.MigratePostgresDataAsync<TestData>(["no-data.seed.yaml"], DbMigration.ConfigureMigrationArgs).ConfigureAwait(false);
// #endif
        await Test.ClearFusionCacheAsync().ConfigureAwait(false);
    }

    [TestCase("/health/live")]
    [TestCase("/health/startup")]
    [TestCase("/health/ready")]
    public void Health_Basic(string path)
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
        string[] _paths =
        [
// #if refdata-enabled
            "$.entries.reference-data-orchestrator",
// #endif
            "$.entries['stackExchange.Redis']",
// #if implement-sqlserver
            "$.entries.sqlServer",
// #elif implement-postgres
            "$.entries.postgreSql",
// #endif
// #if implement-servicebus
            "$.entries.azure-service-bus-session-receiver"
// #endif
        ];

        var r = Test.Http()
            .Run(HttpMethod.Get, path)
            .AssertContentTypeJson();

        r.Response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable);

        var json = r.GetContent().Should().BeJson();
        if (minimal)
            json.NotContainAny(_paths);
        else
            json.ContainAll(_paths);
    }

    [Test]
    public void HostedService_Pause_And_Resume()
    {
// #if implement-servicebus
        const string Service = "azure-service-bus-session-receiver";
// #endif

        Test.Http<string>()
            .Run(HttpMethod.Get, $"/hosted-services/{Service}/status")
            .Value.Should().BeOneOf("Running", "Sleeping");

        Test.Http()
            .Run(HttpMethod.Post, $"/hosted-services/{Service}/pause")
            .Response.StatusCode.Should().Be(HttpStatusCode.Accepted);

        Test.Delay(TimeSpan.FromSeconds(1))
            .Http<string>()
            .Run(HttpMethod.Get, $"/hosted-services/{Service}/status")
            .Value.Should().Be("Paused");

        Test.Http()
            .Run(HttpMethod.Post, $"/hosted-services/{Service}/resume")
            .Response.StatusCode.Should().Be(HttpStatusCode.Accepted);

        Test.Delay(TimeSpan.FromSeconds(1))
            .Http<string>()
            .Run(HttpMethod.Get, $"/hosted-services/{Service}/status")
            .Value.Should().BeOneOf("Running", "Sleeping");
    }
}