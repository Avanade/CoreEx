namespace solution-name.Test.Relay;

public partial class HostTests : WithApiTester<solution-name.Relay.Program>
{
    [OneTimeSetUp]
    public async Task OneTimeSetUpAsync()
    {
// #if implement-sqlserver
        await Test.MigrateSqlServerDataAsync<TestData>(["read-data.seed.yaml"], DbMigration.ConfigureMigrationArgs).ConfigureAwait(false);
// #elif implement-postgres
        await Test.MigratePostgresDataAsync<TestData>(["read-data.seed.yaml"], DbMigration.ConfigureMigrationArgs).ConfigureAwait(false);
// #endif
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
// #if implement-sqlserver
            "$.entries.sqlServer",
            "$.entries.sqlserver-outbox-relay-00",
            "$.entries.sqlserver-outbox-relay-01",
            "$.entries.sqlserver-outbox-relay-02",
            "$.entries.sqlserver-outbox-relay-03"
// #elif implement-postgres
            "$.entries.postgreSql",
            "$.entries.postgres-outbox-relay-00",
            "$.entries.postgres-outbox-relay-01",
            "$.entries.postgres-outbox-relay-02",
            "$.entries.postgres-outbox-relay-03"
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
// #if implement-sqlserver
        const string relay = "sqlserver-outbox-relay-03";
// #elif implement-postgres
        const string relay = "postgres-outbox-relay-03";
// #endif

        Test.Http<string>()
            .Run(HttpMethod.Get, $"/hosted-services/{relay}/status")
            .Value.Should().BeOneOf("Running", "Sleeping");

        Test.Http()
            .Run(HttpMethod.Post, $"/hosted-services/{relay}/pause")
            .Response.StatusCode.Should().Be(HttpStatusCode.Accepted);

        Test.Delay(TimeSpan.FromSeconds(1))
            .Http<string>()
            .Run(HttpMethod.Get, $"/hosted-services/{relay}/status")
            .Value.Should().Be("Paused");

        Test.Http()
            .Run(HttpMethod.Post, $"/hosted-services/{relay}/resume")
            .Response.StatusCode.Should().Be(HttpStatusCode.Accepted);

        Test.Delay(TimeSpan.FromSeconds(1))
            .Http<string>()
            .Run(HttpMethod.Get, $"/hosted-services/{relay}/status")
            .Value.Should().BeOneOf("Running", "Sleeping");
    }
}