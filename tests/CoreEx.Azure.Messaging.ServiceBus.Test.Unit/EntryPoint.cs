using CoreEx.Azure.Messaging.ServiceBus.Test.Unit.Subscribers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CoreEx.Azure.Messaging.ServiceBus.Test.Unit;

public class EntryPoint
{
    public static void ConfigureApplication(IHostApplicationBuilder builder)
    {
        // Add CoreEx host settings.
        builder.AddHostSettings("CoreEx.Azure.Messaging.ServiceBus", "UnitTest", new Uri("urn:unit-test"));

        // Add CoreEx services.
        builder.Services
            .AddExecutionContext()
            .AddEventFormatter()
            .AddFixedDestinationProvider("unit-test")
            .AddAzureServiceBusPublisher();

        // Add azure service bus client using aspire.
        builder.AddAzureServiceBusClient("ServiceBus");

        // Add Receiving and Subscribing services.
        builder.Services
            .AddScoped<ProductSubscriber>()
            .AddSubscribedManager((_, mgr) => mgr.AddSubscriber<ProductSubscriber>())
            .AzureServiceBusReceiving()
                .WithReceiver(_ => ServiceBusReceiverOptions.CreateForTopicSubscription("unit-test", "default"))
                .WithSubscribedSubscriber((_, c) => c.ErrorHandler.Add<InvalidOperationException>(Events.Subscribing.ErrorHandling.Catastrophic))
                .Build();
    }
}