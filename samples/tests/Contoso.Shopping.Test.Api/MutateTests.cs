namespace Contoso.Shopping.Test.Api;

public partial class MutateTests : WithApiTester<Contoso.Shopping.Api.Program>
{
    private static readonly string[] _pathsToIgnore = ["items.etag"];

    private UnitTestEx.Mocking.MockHttpClientRequest _mockHttpReserveRequest = null!;
    private UnitTestEx.Mocking.MockHttpClientRequest _mockHttpCustomerWithAddress = null!;
    private UnitTestEx.Mocking.MockHttpClientRequest _mockHttpCustomerNoAddress = null!;
    private UnitTestEx.Mocking.MockHttpClientRequest _mockHttpCustomerNotFound = null!;

    [SetUp]
    public Task SetUpAsync() => Test.ClearFusionCacheAsync(); // Customers are cached by the adapter; ensure each test starts clean so the Customers mocks are invoked as expected.

    [OneTimeSetUp]
    public async Task OneTimeSetUpAsync()
    {
        // Migrate the database and seed it with test data before starting the test server.
        await Test.MigrateSqlServerDataAsync<TestData>(["mutate-data.seed.yaml"], DbMigration.ConfigureMigrationArgs).ConfigureAwait(false);
        await Test.ClearFusionCacheAsync().ConfigureAwait(false);
        await Test.ResetAzureServiceBusAsync(Common.ServiceBus.GetQueues(), Common.ServiceBus.GetTopicsAndSubscriptions()).ConfigureAwait(false);

        // Use the expected SQL Server Outbox & Azure Service Bus publishers for the tests.
        Test.UseExpectedSqlServerOutboxPublisher();
        Test.UseExpectedAzureServiceBusPublisher();

        // Mock the HTTP clients; the Customers domain is external, so is always mocked (the ids align with the Customers 'read-data.seed.yaml' golden values).
        var mcf = MockHttpClientFactory.Create();
        _mockHttpReserveRequest = mcf.CreateClient("ProductsApi").Request(HttpMethod.Post, "api/inventory/reserve");

        var customers = mcf.CreateClient("CustomersApi");
        _mockHttpCustomerWithAddress = customers.Request(HttpMethod.Get, $"api/customers/{CustomerWithAddressId}");
        _mockHttpCustomerNoAddress = customers.Request(HttpMethod.Get, $"api/customers/{CustomerNoAddressId}");
        _mockHttpCustomerNotFound = customers.Request(HttpMethod.Get, $"api/customers/{CustomerNotFoundId}");
        Test.ReplaceHttpClientFactory(mcf);
    }

    /// <summary>Frank Foster (^16) has a complete shipping address.</summary>
    private static readonly string CustomerWithAddressId = 16.ToGuid().ToString();

    /// <summary>Alice Anderson (^11) has no shipping address.</summary>
    private static readonly string CustomerNoAddressId = 11.ToGuid().ToString();

    private static readonly string CustomerNotFoundId = 404.ToGuid().ToString();

    /// <summary>Mocks the Customers API response for the golden (^16) customer with a complete shipping address.</summary>
    private void MockCustomerWithAddress() => _mockHttpCustomerWithAddress.Respond.WithJson(new
    {
        id = CustomerWithAddressId,
        firstName = "Frank",
        lastName = "Foster",
        email = "frank.foster@example.com",
        phone = "555-0100",
        shippingAddress = new { street1 = "1 Test Street", city = "Springfield", postCode = "49007", state = "IL" },
        customerTypeCode = "IND",
        contactMethodCode = "EM"
    });

    /// <summary>Mocks the Customers API response for the golden (^11) customer with no shipping address.</summary>
    private void MockCustomerNoAddress() => _mockHttpCustomerNoAddress.Respond.WithJson(new
    {
        id = CustomerNoAddressId,
        firstName = "Alice",
        lastName = "Anderson",
        email = "alice.anderson@example.com",
        customerTypeCode = "IND",
        contactMethodCode = "EM"
    });
}
