using CoreEx.Database.SqlServer.Outbox;

namespace Contoso.Shopping.Test.Relay;

public class RelayTests : WithApiTester<Contoso.Shopping.Relay.Program>
{
    [OneTimeSetUp]
    public async Task OneTimeSetUpAsync()
    {
        await Test.MigrateSqlServerDataAsync<TestData>(["no-data.seed.yaml"], DbMigration.ConfigureMigrationArgs).ConfigureAwait(false);
        await Test.ResetAzureServiceBusAsync(Common.ServiceBus.GetQueues(), Common.ServiceBus.GetTopicsAndSubscriptions()).ConfigureAwait(false);
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

    [Test]
    public void Outbox_Relay_Command_Queue()
    {
        // Arrange the two commands to publish and relay to the per-domain command queue.
        var ce1 = Test.CreateCloudEventFromJsonResource("BasketCreatedCloudEvent.json");
        var ce2 = Test.CreateCloudEventFromJsonResource("BasketUpdatedCloudEvent.json");

        Test.ScopedType<ExecutionContext>(test =>
        {
            test.Run(async _ =>
            {
                // Publish to the queue destination (as resolved by the NamedDestinationProvider for a command to the products domain).
                var pub = ActivatorUtilities.GetServiceOrCreateInstance<SqlServerOutboxPublisher>(test.Services);
                pub.Add("contoso-products", [ce1, ce2]);
                await pub.PublishAsync();

                // Poll (rather than sleep a fixed time) as the hosted-service relay latency varies; receive the messages from the queue and assert. The relay uses the persisted destination so needs no destination provider.
                var list = new List<Azure.Messaging.ServiceBus.ServiceBusReceivedMessage>();
                for (int i = 0; i < 30 && list.Count < 2; i++)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1));
                    list.AddRange(await Test.GetAndClearAzureServiceBusAsync(ServiceBusSessionReceiverOptions.CreateForQueue("contoso-products")));
                }

                list.Should().NotBeNull().And.HaveCount(2);
                list.Should().ContainSingle(x => x.MessageId == ce1.Id);
                list.Should().ContainSingle(x => x.MessageId == ce2.Id);
            }).AssertSuccess();
        });
    }
}
