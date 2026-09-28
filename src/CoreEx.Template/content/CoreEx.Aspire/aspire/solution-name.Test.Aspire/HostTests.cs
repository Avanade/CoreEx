namespace solution-name.Test.Aspire;

public class HostTests : WithAspireTester<Projects.solution-name-underscore_Aspire>
{
    [OneTimeSetUp]
    public async Task OneTimeSetUpAsync()
    {
// #if implement-sqlserver
        await Test.MigrateSqlServerDataAsync<TestData>("SqlServer", ["no-data.seed.yaml"], DbMigration.ConfigureMigrationArgs);
// #elif implement-postgres
        await Test.MigratePostgresDataAsync<TestData>("Postgres", ["no-data.seed.yaml"], DbMigration.ConfigureMigrationArgs);
// #endif

        // Clear the Redis cache.
        await Test.ClearRedisCacheAsync("redis");

// #if implement-servicebus
        // Reset the Azure Service Bus topic/subscription to an initial state.
        await Test.ResetAzureServiceBusAsync("ServiceBus", null,
        [
            (new CreateTopicOptions("domain-parent-lower"), [new CreateSubscriptionOptions("domain-parent-lower", "domain-name-lower") { RequiresSession = true }])
        ]);
// #endif

        // Hang about until the host(s) are available.
        await Test.WaitForResourceAsync([
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
