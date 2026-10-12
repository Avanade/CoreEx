namespace Contoso.Shopping.Test.Subscribe;

/// <summary>
/// NOTE: Using the ServiceBusSubscribedSubscriber bypasses the need to actually send a message to the Service Bus, instead it simulates the receive of a message.
/// </summary>
public partial class SubscriberTests : WithApiTester<Contoso.Shopping.Subscribe.Program>
{
    private MockHttpClientRequest _mockHttpSendMailRequest = null!;
    private MockHttpClientRequest _mockHttpGetCustomerRequest = null!;

    [SetUp]
    public Task SetUpAsync() => Test.ClearFusionCacheAsync(); // Customers are cached by the adapter; ensure each test starts clean so the Customers mock is invoked as expected.

    [OneTimeSetUp]
    public async Task OneTimeSetUpAsync()
    {
        await Test.MigrateSqlServerDataAsync<TestData>(["mutate-data.seed.yaml"], DbMigration.ConfigureMigrationArgs).ConfigureAwait(false);
        await Test.ClearFusionCacheAsync().ConfigureAwait(false);
        await Test.ResetAzureServiceBusAsync(Common.ServiceBus.GetQueues(), Common.ServiceBus.GetTopicsAndSubscriptions()).ConfigureAwait(false);

        Test.UseExpectedSqlServerOutboxPublisher();

        // Mock the HTTP clients (SendGrid and CustomersApi are exercised in tests; ProductsApi is registered so IProductAdapter can still be resolved by other tests in this suite).
        var mcf = UnitTestEx.MockHttpClientFactory.Create();
        mcf.CreateClient("ProductsApi");
        _mockHttpGetCustomerRequest = mcf.CreateClient("CustomersApi").Request(HttpMethod.Get, $"api/customers/{16.ToGuid()}");
        _mockHttpSendMailRequest = mcf.CreateClient("SendGrid").Request(HttpMethod.Post, "v3/mail/send");
        Test.ReplaceHttpClientFactory(mcf);
    }

    [Test]
    public void Unsubscribed_Error() => Test.Scoped(test =>
    {
        var ed = EventData.CreateEvent("test", "not-subscribed").WithKey("abc");
        var ce = Test.CreateCloudEventFrom(ed);
        var sbm = ce.ToServiceBusReceivedMessage();

        test.Run(async _ =>
        {
            var sbs = test.Services.GetRequiredService<ServiceBusSubscribedSubscriber>();
            var r = await sbs.ReceiveAsync(sbm);

            r.IsFailure.Should().BeTrue();
            var e = r.Error.Should().BeOfType<EventSubscriberHandledException>().Subject;
            e.ErrorHandling.Should().Be(ErrorHandling.CompleteAsSilent);
            e.InnerException.Should().NotBeNull();
            e.InnerException.Message.Should().Be("No subscriber matched the event.");
        }).AssertSuccess();
    });
}
