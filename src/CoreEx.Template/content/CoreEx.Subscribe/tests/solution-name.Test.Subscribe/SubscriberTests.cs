namespace solution-name.Test.Subscribe;

public partial class SubscriberTests : WithApiTester<solution-name.Subscribe.Program>
{
    [OneTimeSetUp]
    public async Task OneTimeSetUpAsync()
    {
        // #if implement-sqlserver
        await Test.MigrateSqlServerDataAsync<TestData>(["mutate-data.seed.yaml"], DbMigration.ConfigureMigrationArgs).ConfigureAwait(false);
        // #elif implement-postgres
        await Test.MigratePostgresDataAsync<TestData>(["mutate-data.seed.yaml"], DbMigration.ConfigureMigrationArgs).ConfigureAwait(false);
        // #endif
        await Test.ClearFusionCacheAsync().ConfigureAwait(false);

        // #if implement-sqlserver
        Test.UseExpectedSqlServerOutboxPublisher();
        // #elif implement-postgres
        Test.UseExpectedPostgresOutboxPublisher();
        // #endif
    }

    [Test]
    public void NotSubscribed_CompleteAsSilent() => Test.Scoped(test =>
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