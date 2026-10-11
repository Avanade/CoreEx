namespace solution-name.Test.Aspire;

public class HostTests : WithAspireTester<Projects.solution-name-underscore_Aspire>
{
    protected override async Task OnBeforeStartAsync(DistributedApplication app)
    {
// #if implement-sqlserver
        await app.MigrateSqlServerDataAsync<TestData>("SqlServer", ["no-data.seed.yaml"], DbMigration.ConfigureMigrationArgs);
// #elif implement-cosmos
        await app.MigrateCosmosDataAsync<TestData>("Cosmos", "domain-name-lower", ["no-data.seed.yaml"], DbMigration.ConfigureProvisionArgs);
// #elif implement-postgres
        await app.MigratePostgresDataAsync<TestData>("Postgres", ["no-data.seed.yaml"], DbMigration.ConfigureMigrationArgs);
// #endif

        // Clear the Redis cache.
        await app.ClearRedisCacheAsync("redis");

// #if implement-servicebus
        // Reset the Azure Service Bus queues and topics/subscriptions to an initial state (see the Test.Common ServiceBus).
        await app.ResetAzureServiceBusAsync("ServiceBus", Common.ServiceBus.GetQueues(), Common.ServiceBus.GetTopicsAndSubscriptions());
// #endif
    }

    protected override async Task OnAfterStartAsync(DistributedApplication app)
    {
        // Hang about until the host(s) are available.
        await app.WaitForResourceAsync([
// #if has-api
            "domain-name-lower-api",
// #endif
// #if has-relay
            "domain-name-lower-relay",
// #endif
// #if has-subscribe
            "domain-name-lower-subscribe",
// #endif
        ]);
    }

// #if has-api
    [TestCase("/health/live")]
    [TestCase("/health/ready")]
    public Task Api_Health(string path) => AssertHealthyAsync("domain-name-lower-api", path);
// #endif

// #if has-relay
    [Test]
    public Task Relay_Health() => AssertHealthyAsync("domain-name-lower-relay", "/health/ready");
// #endif

// #if has-subscribe
    [Test]
    public Task Subscribe_Health() => AssertHealthyAsync("domain-name-lower-subscribe", "/health/ready");
// #endif

    private async Task AssertHealthyAsync(string resourceName, string path)
    {
        for (var attempt = 0; attempt < 60; attempt++)
        {
            var response = Test.Http(resourceName).Run(HttpMethod.Get, path);
            if (response.Response.StatusCode != HttpStatusCode.ServiceUnavailable || attempt == 59)
            {
                response.Response.StatusCode.Should().Be(HttpStatusCode.OK, $"{resourceName} {path} must report healthy");
                return;
            }

            await Task.Delay(TimeSpan.FromSeconds(1));
        }
    }
}
