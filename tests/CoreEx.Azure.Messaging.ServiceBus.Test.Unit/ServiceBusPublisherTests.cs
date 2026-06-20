using CoreEx.Events.Publishing;
using Microsoft.Extensions.DependencyInjection;
using UnitTestEx.Expectations;

namespace CoreEx.Azure.Messaging.ServiceBus.Test.Unit;

public class ServiceBusPublisherTests : WithGenericTester<EntryPoint>
{
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
}