namespace Contoso.Shopping.Test.Subscribe;

/// <summary>
/// NOTE: Using the ServiceBusSubscribedSubscriber bypasses the need to actually send a message to the Service Bus, instead it simulates the receive of a message.
/// </summary>
public partial class SubscriberTests : WithApiTester<Contoso.Shopping.Subscribe.Program>
{
    [OneTimeSetUp]
    public async Task OneTimeSetUpAsync()
    {
        await Test.MigrateSqlServerDataAsync<TestData>(["mutate-data.seed.yaml"], DbMigration.ConfigureMigrationArgs).ConfigureAwait(false);
        await Test.ClearFusionCacheAsync().ConfigureAwait(false);

        Test.UseExpectedSqlServerOutboxPublisher();
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