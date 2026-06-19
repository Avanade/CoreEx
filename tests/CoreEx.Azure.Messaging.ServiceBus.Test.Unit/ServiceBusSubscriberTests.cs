using CoreEx.Events;
using Microsoft.Extensions.DependencyInjection;
using UnitTestEx.Expectations;

namespace CoreEx.Azure.Messaging.ServiceBus.Test.Unit;

public class ServiceBusSubscriberTests : WithGenericTester<EntryPoint>
{
    [Test]
    public void ReceiveAsync_SingleMessage() => Test.ScopedType<ExecutionContext>(test =>
    {
        // Arrange the message.
        var ed = EventData.CreateEventWith(new Subscribers.Product { Id = 1, Sku = "SKU-001" }, "Created");
        var ef = test.Services.GetRequiredService<IEventFormatter>();
        var ce = ef.ConvertToCloudEvent(ef.Format(ed));
        var sm = ce.ToServiceBusMessage();
        var msg = ServiceBusMessageTests.ConvertToReceivedMessage(sm);

        // Act & Assert.
        test.ExpectLogContains("Received product with Id: 1 and Sku: SKU-001.")
            .Run(async _ =>
            {
                var sbss = Test.Services.GetRequiredService<ServiceBusSubscribedSubscriber>();
                var result = await sbss.ReceiveAsync(msg!).ConfigureAwait(false);
                result.IsSuccess.Should().BeTrue();
            }).AssertSuccess();
    });
}
