using CoreEx.Database.Postgres.Outbox;

namespace Contoso.Products.Test.Relay;

public class RelayTests : WithApiTester<Contoso.Products.Relay.Program>
{
    [OneTimeSetUp]
    public async Task OneTimeSetUpAsync()
    {
        await Test.MigratePostgresDataAsync<TestData>(["no-data.seed.yaml"], DbMigration.ConfigureMigrationArgs).ConfigureAwait(false);
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

                // Poll for delivery because relay polling can vary under load.
                var list = new List<Azure.Messaging.ServiceBus.ServiceBusReceivedMessage>();
                for (int i = 0; i < 30 && list.Count < 2; i++)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1));
                    list.AddRange(await Test.GetAndClearAzureServiceBusAsync(ServiceBusSessionReceiverOptions.CreateForTopicSubscription("contoso", "products")));
                }

                list.Should().NotBeNull().And.HaveCount(2);
                var ce1Msg = list.Should().ContainSingle(x => x.MessageId == ce1.Id).Subject;
                var ce2Msg = list.Should().ContainSingle(x => x.MessageId == ce2.Id).Subject;
                ObjectComparer.AssertJson(ce1.EncodeToJsonElement().ToString(), ce1Msg.Body.ToString());
                ObjectComparer.AssertJson(ce2.EncodeToJsonElement().ToString(), ce2Msg.Body.ToString());
            }).AssertSuccess();
        });
    }

    [Test]
    public void Outbox_Relay_NativeJsonAndOpaque_WithRollback()
    {
        Test.ScopedType<ExecutionContext>(test => test.Run(async _ =>
        {
            var publisher = ActivatorUtilities.GetServiceOrCreateInstance<PostgresOutboxPublisher>(test.Services);
            var database = test.Services.GetRequiredService<CoreEx.Database.Postgres.PostgresDatabase>();
            var unitOfWork = new CoreEx.Database.Postgres.PostgresUnitOfWork(database, publisher);
            var key = Guid.NewGuid().ToString();
            var rolledBack = EventData.CreateNativeWith(new { ETag = "rolled-back" }).WithKey(key);
            var rollbackUnitOfWork = new CoreEx.Database.Postgres.PostgresUnitOfWork(database);
            Func<Task> rollback = () => rollbackUnitOfWork.TransactionAsync(async ct =>
            {
                publisher.Add("contoso", rolledBack);
                await publisher.PublishAsync(ct).ConfigureAwait(false);
                throw new InvalidOperationException("Rollback native outbox.");
            });

            await rollback.Should().ThrowAsync<InvalidOperationException>().WithMessage("Rollback native outbox.");
            publisher.Reset();

            var json = EventData.CreateNativeWith(new { ETag = "keep" }).WithKey(key).WithTitle("native.json");
            json.CorrelationId = "corr";
            json.CausationId = "cause";
            json.SetAttribute("count", "1");
            json.SetAttribute("amount", "1.5");
            json.SetAttribute("ratio", "1.5");
            json.SetAttribute("enabled", "true");
            json.SetAttribute("code", "external");
            json.SetAttribute("revision", "2");
            json.ReplyTo = "partner-replies";
            var opaque = EventData.CreateNativeWithData(BinaryData.FromString("{ \"etag\": \"exact\" }\n", "application/json")).WithKey(key).WithTitle("native.opaque");
            var cloudEvent = EventData.CreateEventWith(new { Value = 1 }, "created").WithKey(key);
            await unitOfWork.TransactionAsync(ct =>
            {
                publisher.Add("contoso", json, opaque, cloudEvent);
                return Task.CompletedTask;
            }).ConfigureAwait(false);

            var received = new List<Azure.Messaging.ServiceBus.ServiceBusReceivedMessage>();
            for (var i = 0; i < 30 && received.Count < 3; i++)
            {
                await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
                received.AddRange(await Test.GetAndClearAzureServiceBusAsync(ServiceBusSessionReceiverOptions.CreateForTopicSubscription("contoso", "products")).ConfigureAwait(false));
            }

            received.Should().HaveCount(3).And.NotContain(x => x.MessageId == rolledBack.Id);
            var jsonMessage = received.Single(x => x.MessageId == json.Id);
            jsonMessage.IsCloudEvent().Should().BeFalse();
            jsonMessage.Body.ToString().Should().Contain("\"etag\":\"keep\"");
            jsonMessage.ApplicationProperties["count"].Should().Be("1");
            jsonMessage.CorrelationId.Should().Be("corr");
            jsonMessage.ReplyTo.Should().Be("partner-replies");
            foreach (var property in json.Attributes)
            {
                jsonMessage.ApplicationProperties[property.Key].Should().Be(property.Value);
                jsonMessage.ApplicationProperties[property.Key].Should().BeOfType<string>();
            }

            jsonMessage.Subject.Should().Be("native.json");
            jsonMessage.SessionId.Should().Be(CoreEx.Data.PartitionKey.GetPartitionIdAsString(key));
            received.Single(x => x.MessageId == opaque.Id).Body.ToArray().Should().Equal(opaque.Data!.ToArray());
            received.Single(x => x.MessageId == cloudEvent.Id).IsCloudEvent().Should().BeTrue();
        }).AssertSuccess());
    }
}
