// #if (implement-servicebus && implement-sqlserver)
using CoreEx.Database.SqlServer.Outbox;

// #elif (implement-servicebus && implement-postgres)
using CoreEx.Database.Postgres.Outbox;

// #endif
namespace solution-name.Test.Relay;

public class RelayTests : WithApiTester<solution-name.Relay.Program>
{
    [OneTimeSetUp]
    public async Task OneTimeSetUpAsync()
    {
// #if implement-sqlserver
        await Test.MigrateSqlServerDataAsync<TestData>(["no-data.seed.yaml"], DbMigration.ConfigureMigrationArgs).ConfigureAwait(false);
// #elif implement-postgres
        await Test.MigratePostgresDataAsync<TestData>(["no-data.seed.yaml"], DbMigration.ConfigureMigrationArgs).ConfigureAwait(false);
// #endif
// #if implement-servicebus
        await Test.GetAndClearAzureServiceBusAsync(ServiceBusSessionReceiverOptions.CreateForTopicSubscription("domain-parent-lower", "domain-name-lower"));
// #endif
    }

// #if implement-servicebus
    [Test]
    public void Outbox_Relay()
    {
        // Arrange the two events to publish and relay.
        var ce1 = Test.CreateCloudEventFromJsonResource("Test-CloudEvent-00.json");
        var ce2 = Test.CreateCloudEventFromJsonResource("Test-CloudEvent-01.json");

        // Publish two events to the outbox.
        Test.ScopedType<ExecutionContext>(test =>
        {
            test.Run(async _ =>
            {
                // Publish two events to the outbox.
// #if implement-sqlserver
                var pub = ActivatorUtilities.GetServiceOrCreateInstance<SqlServerOutboxPublisher>(test.Services);
// #elif implement-postgres
                var pub = ActivatorUtilities.GetServiceOrCreateInstance<PostgresOutboxPublisher>(test.Services);
// #endif
                pub.Add("domain-parent-lower", [ce1, ce2]);
                await pub.PublishAsync();

                // Hosted-service(s) are currently running and should relay to Azure Service Bus; we just need to give it a few seconds to do so.
                for (int i = 0; i < 5; i++)
                    await Task.Delay(TimeSpan.FromSeconds(1));

                // Receive the events from Azure Service Bus and assert (order is not guaranteed as different sessions).
                var list = await Test.GetAndClearAzureServiceBusAsync(ServiceBusSessionReceiverOptions.CreateForTopicSubscription("domain-parent-lower", "domain-name-lower"));

                list.Should().NotBeNull().And.HaveCount(2);
                list.Should().NotBeNull().And.HaveCount(2);
                var ce1Msg = list.Should().ContainSingle(x => x.MessageId == ce1.Id).Subject;
                var ce2Msg = list.Should().ContainSingle(x => x.MessageId == ce2.Id).Subject;
                ObjectComparer.AssertJson(ce1.EncodeToJsonElement().ToString(), ce1Msg.Body.ToString());
                ObjectComparer.AssertJson(ce2.EncodeToJsonElement().ToString(), ce2Msg.Body.ToString());
            }).AssertSuccess();
        });
    }
// #endif
}