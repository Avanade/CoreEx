namespace Contoso.Shopping.Test.Api;

public partial class MutateTests : WithApiTester<Contoso.Shopping.Api.Program>
{
    private static readonly string[] _pathsToIgnore = ["items.etag"];

    private UnitTestEx.Mocking.MockHttpClientRequest _mockHttpReserveRequest = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUpAsync()
    {
        // Migrate the database and seed it with test data before starting the test server.
        await Test.MigrateSqlServerDataAsync<TestData>(DbMigration.ConfigureMigrationArgs).ConfigureAwait(false);
        await Test.ClearFusionCacheAsync().ConfigureAwait(false);

        // Use the expected SQL Server Outbox & Azure Service Bus publishers for the tests.
        Test.UseExpectedSqlServerOutboxPublisher();
        Test.UseExpectedAzureServiceBusPublisher();

        // Mock the HTTP client.
        var mcf = MockHttpClientFactory.Create();
        _mockHttpReserveRequest = mcf.CreateClient("ProductsApi").Request(HttpMethod.Post, "api/inventory/reserve");
        Test.ReplaceHttpClientFactory(mcf);
    }
}