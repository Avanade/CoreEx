using CoreEx.Database.Postgres.Outbox;

namespace Contoso.Products.Test.Outbox.Relay;

public class RelayTests : WithApiTester<Contoso.Products.Outbox.Relay.Program>
{
    [OneTimeSetUp]
    public async Task OneTimeSetUpAsync()
    {
        await Test.MigratePostgresDataAsync<TestData>(DbMigration.ConfigureMigrationArgs).ConfigureAwait(false);
        await Test.GetAndClearAzureServiceBusAsync(ServiceBusSessionReceiverOptions.CreateForTopicSubscription("contoso", "products"));
    }

    [Test]
    public void Outbox_Relay()
    {
        // Arrange the two events to publish and relay.
        var ce1 = Test.CreateCloudEventFromJsonResource("ProductCreatedCloudEvent.json");
        var ce2 = Test.CreateCloudEventFromJsonResource("ProductDeletedCloudEvent.json");

        // Publish two events to the outbox.
        Test.ScopedType<ExecutionContext>(test =>
        {
            test.Run(async _ =>
            {
                // Publish two events to the outbox.
                var pub = ActivatorUtilities.GetServiceOrCreateInstance<PostgresOutboxPublisher>(test.Services);
                pub.Add("contoso", [ce1, ce2]);
                await pub.PublishAsync();

                // Hosted-service(s) are currently running and should relay to Azure Service Bus; we just need to give it a few seconds to do so.
                for (int i = 0; i < 5; i++)
                    await Task.Delay(TimeSpan.FromSeconds(1));

                // Receive the events from Azure Service Bus and assert.
                var list = await Test.GetAndClearAzureServiceBusAsync(ServiceBusSessionReceiverOptions.CreateForTopicSubscription("contoso", "products"));

                list.Should().NotBeNull().And.HaveCount(2);
                list.Should().ContainSingle(x => x.MessageId == ce1.Id);
                ObjectComparer.AssertJson(ce1.EncodeToJsonElement().ToString(), list.Single(x => x.MessageId == ce1.Id).Body.ToString());
                ObjectComparer.AssertJson(ce2.EncodeToJsonElement().ToString(), list.Single(x => x.MessageId == ce2.Id).Body.ToString());
            }).AssertSuccess();
        });
    }
}