using CoreEx.Database.SqlServer.Outbox;

namespace Contoso.Shopping.Test.Relay;

public class RelayTests : WithApiTester<Contoso.Shopping.Relay.Program>
{
    [OneTimeSetUp]
    public async Task OneTimeSetUpAsync()
    {
        await Test.MigrateSqlServerDataAsync<TestData>(["no-data.seed.yaml"], DbMigration.ConfigureMigrationArgs).ConfigureAwait(false);
        await Test.GetAndClearAzureServiceBusAsync(ServiceBusSessionReceiverOptions.CreateForTopicSubscription("contoso", "shopping"));
    }

    [Test]
    public void Outbox_Relay()
    {
        // Arrange the two events to publish and relay.
        var ce1 = Test.CreateCloudEventFromJsonResource("BasketCreatedCloudEvent.json");
        var ce2 = Test.CreateCloudEventFromJsonResource("BasketUpdatedCloudEvent.json");

        // Publish two events to the outbox.
        Test.ScopedType<ExecutionContext>(test =>
        {
            test.Run(async _ =>
            {
                // Publish two events to the outbox.
                var pub = ActivatorUtilities.GetServiceOrCreateInstance<SqlServerOutboxPublisher>(test.Services);
                pub.Add("contoso", [ce1, ce2]);
                await pub.PublishAsync();

                // Hosted-service(s) are currently running and should relay to Azure Service Bus; we just need to give it a few seconds to do so.
                for (int i = 0; i < 5; i++)
                    await Task.Delay(TimeSpan.FromSeconds(1));

                // Receive the events from Azure Service Bus and assert.
                var list = await Test.GetAndClearAzureServiceBusAsync(ServiceBusSessionReceiverOptions.CreateForTopicSubscription("contoso", "shopping"));

                list.Should().NotBeNull().And.HaveCount(2);
                var ce1Msg = list.Should().ContainSingle(x => x.MessageId == ce1.Id).Subject;
                var ce2Msg = list.Should().ContainSingle(x => x.MessageId == ce2.Id).Subject;
                ObjectComparer.AssertJson(ce1.EncodeToJsonElement().ToString(), ce1Msg.Body.ToString());
                ObjectComparer.AssertJson(ce2.EncodeToJsonElement().ToString(), ce2Msg.Body.ToString());
            }).AssertSuccess();
        });
    }
}
