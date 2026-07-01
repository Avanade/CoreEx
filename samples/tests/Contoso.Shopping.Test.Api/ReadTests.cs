namespace Contoso.Shopping.Test.Api;

public partial class ReadTests : WithApiTester<Contoso.Shopping.Api.Program>
{
    private static readonly string[] _pathsToIgnore = ["items.etag"];

    [OneTimeSetUp]
    public async Task OneTimeSetUpAsync()
    {
        await Test.MigrateSqlServerDataAsync<TestData>(["read-data.seed.yaml"], DbMigration.ConfigureMigrationArgs).ConfigureAwait(false);
        await Test.ClearFusionCacheAsync().ConfigureAwait(false);

        // Use the expected SQL Server Outbox & Azure Service Bus publishers for the tests.
        Test.UseExpectedSqlServerOutboxPublisher();
        Test.UseExpectedAzureServiceBusPublisher();
    }
}