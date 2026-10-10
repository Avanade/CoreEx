using Azure.Messaging.ServiceBus;
using CoreEx.Azure.Messaging.ServiceBus;
using CoreEx.Events.Publishing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UnitTestEx.Expectations;

namespace CoreEx.Azure.Messaging.ServiceBus.Test.Unit;

public class ServiceBusPublisherTests : WithGenericTester<EntryPoint>
{
    [Test]
    public void AddAzureServiceBusPublisher_UsesIndependentSessionPartitionSize()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["CoreEx:Host:Outbox:PartitionSize"] = "2",
                [ServiceBusPublisher.SessionIdPartitionSizeConfigurationKey] = "8",
                ["CoreEx:Host:ServiceBus:Destinations:contoso:SessionIdStrategy"] = "UsePartitionKeyAsIs",
                ["CoreEx:Host:ServiceBus:Destinations:contoso-products:SessionIdStrategy"] = "UsePartitionKeyConvertedToAnId",
                ["CoreEx:Host:ServiceBus:Destinations:contoso-products:SessionIdPartitionSize"] = "2"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();
        services.AddSingleton(new ServiceBusClient("Endpoint=sb://localhost/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA="));
        services.AddAzureServiceBusPublisher((_, publisher) =>
        {
            publisher.SessionIdStrategy = ServiceBusSessionStrategy.UsePartitionKeyConvertedToAnId;
            publisher.DestinationSettings["contoso"] = new() { SessionIdPartitionSize = 4 };
        });
        using var provider = services.BuildServiceProvider();

        var publisher = (ServiceBusPublisher)provider.GetRequiredKeyedService<IEventPublisher>(ServiceBusPublisher.DefaultServiceKey);
        publisher.SessionIdPartitionSize.Should().Be(8);
        publisher.DestinationSettings["contoso"].SessionIdStrategy.Should().Be(ServiceBusSessionStrategy.UsePartitionKeyAsIs);
        publisher.DestinationSettings["contoso"].SessionIdPartitionSize.Should().Be(4);
        publisher.DestinationSettings["contoso-products"].SessionIdStrategy.Should().Be(ServiceBusSessionStrategy.UsePartitionKeyConvertedToAnId);
        publisher.DestinationSettings["contoso-products"].SessionIdPartitionSize.Should().Be(2);
    }

    [Test]
    public void PublishAsync_SingleBatchOfOne() => Test.ScopedType<ExecutionContext>(test =>
    {
        test.ExpectLogContains("Sending batch of 1 event(s) to destination 'unit-test'.")
            .ExpectLogContains("SendAsync start. MessageCount = 1 [Azure.Messaging.ServiceBus]")
            .Run(async ec =>
            {
                var sbp = (ServiceBusPublisher)ec.ServiceProvider!.GetRequiredKeyedService<IEventPublisher>(ServiceBusPublisher.DefaultServiceKey);

                sbp.Add(Events.EventData.CreateEvent("Entity", "Action").WithPartitionKey());

                sbp.SessionIdStrategy = ServiceBusSessionStrategy.None;
                await sbp.PublishAsync();
            }).AssertSuccess();
    });

    [Test]
    public void PublishAsync_MultiBatch() => Test.ScopedType<ExecutionContext>(test =>
    {
        test.ExpectLogContains("Sending batch of 2 event(s) to destination 'unit-test'.")
            .ExpectLogContains("Sending batch of 1 event(s) to destination 'unit-test-2'.")
            .ExpectLogContains("SendAsync start. MessageCount = 2 [Azure.Messaging.ServiceBus]")
            .ExpectLogContains("SendAsync start. MessageCount = 1 [Azure.Messaging.ServiceBus]")
            .Run(async ec =>
            {
                var sbp = (ServiceBusPublisher)ec.ServiceProvider!.GetRequiredKeyedService<IEventPublisher>(ServiceBusPublisher.DefaultServiceKey);

                sbp.Add(Events.EventData.CreateEvent("Entity", "Action1").WithPartitionKey());
                sbp.Add(Events.EventData.CreateEvent("Entity", "Action2").WithPartitionKey());
                sbp.Add("unit-test-2", Events.EventData.CreateEvent("Entity", "Action3").WithPartitionKey());

                sbp.SessionIdStrategy = ServiceBusSessionStrategy.None;
                await sbp.PublishAsync();
            }).AssertSuccess();
    });

    [Test]
    public void PublishAsync_Single_UseSessions() => Test.ScopedType<ExecutionContext>(test =>
    {
        test.ExpectLogContains("Sending batch of 1 event(s) to destination 'unit-test'.")
            .ExpectLogContains("SendAsync start. MessageCount = 1 [Azure.Messaging.ServiceBus]")
            .Run(async ec =>
            {
                var sbp = (ServiceBusPublisher)ec.ServiceProvider!.GetRequiredKeyedService<IEventPublisher>(ServiceBusPublisher.DefaultServiceKey);

                sbp.Add(Events.EventData.CreateEvent("Entity", "Action").WithKey("123"));

                sbp.SessionIdStrategy = ServiceBusSessionStrategy.UsePartitionKeyAsIs;
                await sbp.PublishAsync();
            }).AssertSuccess();
    });

    [Test]
    public void PublishAsync_UseSessions_NoPartitionKey_UsesFixedSessionId() => Test.ScopedType<ExecutionContext>(test =>
    {
        // Regression: a null PartitionKey under UsePartitionKeyAsIs must fall back to the fixed NoPartitionKeySessionId
        // (bounded to at most one extra session) rather than a new random GUID per message (unbounded session growth).
        // Uses a standalone (non-DI) publisher with its own formatter so PartitionKeyIsRequired can be disabled without
        // mutating the shared (singleton-registered) IEventFormatter used by other tests.
        test.ExpectLogContains($"falling back to '{ServiceBusPublisher.DefaultNoPartitionKeySessionId}' for UsePartitionKeyAsIs session assignment.")
            .Run(async ec =>
            {
                var client = ec.ServiceProvider!.GetRequiredService<ServiceBusClient>();
                var logger = ec.ServiceProvider!.GetRequiredService<ILogger<ServiceBusPublisher>>();
                var formatter = new Events.EventFormatter { PartitionKeyIsRequired = false };
                var sbp = new ServiceBusPublisher(client, formatter: formatter, logger: logger) { SessionIdStrategy = ServiceBusSessionStrategy.UsePartitionKeyAsIs };

                sbp.Add("unit-test", Events.EventData.CreateEvent("Entity", "Action"));

                await sbp.PublishAsync();
            }).AssertSuccess();
    });

    [Test]
    public void PublishAsync_UseSessions_UsePartitionKeyConvertedToAnId_NoPartitionKey_UsesFixedValueAsHashInput() => Test.ScopedType<ExecutionContext>(test =>
    {
        // Regression: a null PartitionKey under UsePartitionKeyConvertedToAnId must also use the fixed NoPartitionKeySessionId
        // (as the hash input) rather than a new random GUID per message - this ensures partition-key-less events consistently
        // land in the same bucket/session, preserving their relative publish order (matching UsePartitionKeyAsIs), rather than
        // being scattered randomly across the pool with no ordering guarantee between them.
        test.ExpectLogContains($"falling back to '{ServiceBusPublisher.DefaultNoPartitionKeySessionId}' for UsePartitionKeyConvertedToAnId session assignment.")
            .Run(async ec =>
            {
                var client = ec.ServiceProvider!.GetRequiredService<ServiceBusClient>();
                var logger = ec.ServiceProvider!.GetRequiredService<ILogger<ServiceBusPublisher>>();
                var formatter = new Events.EventFormatter { PartitionKeyIsRequired = false };
                var sbp = new ServiceBusPublisher(client, formatter: formatter, logger: logger) { SessionIdStrategy = ServiceBusSessionStrategy.UsePartitionKeyConvertedToAnId };

                sbp.Add("unit-test", Events.EventData.CreateEvent("Entity", "Action"));

                await sbp.PublishAsync();
            }).AssertSuccess();
    });
}
