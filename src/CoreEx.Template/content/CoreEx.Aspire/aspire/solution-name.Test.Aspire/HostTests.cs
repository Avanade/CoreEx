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
    public void Api_Health(string path)
    {
        Test.Http("domain-name-lower-api")
            .Run(HttpMethod.Get, path)
            .Response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable);
    }
// #endif

// #if has-relay
    [Test]
    public void Relay_Health()
    {
        Test.Http("domain-name-lower-relay")
            .Run(HttpMethod.Get, "/health/ready")
            .Response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable);
    }
// #endif

// #if has-subscribe
    [Test]
    public void Subscribe_Health()
    {
        Test.Http("domain-name-lower-subscribe")
            .Run(HttpMethod.Get, "/health/ready")
            .Response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable);
    }
// #endif
}
