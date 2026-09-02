using Azure.Messaging.ServiceBus;
using CoreEx.Events;
using CoreEx.Events.Publishing;
using CoreEx.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using UnitTestEx.Expectations;

namespace CoreEx.Azure.Messaging.ServiceBus.Test.Unit;

public class ServiceBusReceiverTests : WithGenericTester<EntryPoint>
{
    [SetUp]
    public Task SetUpAsync() => ReceiveAllMessages();

    private async Task ReceiveAllMessages()
    {
        var c = Test.Services.GetRequiredService<ServiceBusClient>();
        await using var receiver = c.CreateReceiver("unit-test", "default");

        for (var i = 0; i < 2; i++)
        {
            while (true)
            {
                var messages = await receiver.ReceiveMessagesAsync(maxMessages: 50, maxWaitTime: TimeSpan.FromMilliseconds(5));
                if (messages.Count == 0)
                    break;

                foreach (var m in messages)
                    await receiver.CompleteMessageAsync(m);
            }

            await Task.Delay(100); // Allow some time for the service bus to reflect the completed messages before trying to receive again.
        }
    }

    [Test]
    public void GetAndClearAzureServiceBusAsync_ReturnsAllPublishedMessages() => Test.ScopedType<ExecutionContext>(async test =>
    {
        // Regression (against a real Service Bus emulator, not a mock): proves GetAndClearAzureServiceBusAsync's internal
        // receive poll reliably drains every message published just beforehand, closing the gap left by CoreEx.UnitTesting's
        // "no live infra to verify" note for this method.
        var sp = (ServiceBusPublisher)test.Services.GetRequiredKeyedService<IEventPublisher>(ServiceBusPublisher.DefaultServiceKey);
        for (var i = 0; i < 10; i++)
            sp.Add(EventData.CreateEventWith(new Subscribers.Product { Id = i, Sku = $"SKU-{i}" }, "Created"));

        await sp.PublishAsync();

        var messages = await Test.GetAndClearAzureServiceBusAsync(ServiceBusReceiverOptions.CreateForTopicSubscription("unit-test", "default"));

        messages.Should().HaveCount(10);
    });

    [Test]
    public void Receiver_Cycle_States() => Test.ScopedType<ExecutionContext>(async test =>
    {
        // Create using the root services (not scoped).
        var sbr = Test.Services.GetRequiredService<ServiceBusReceiver<ServiceBusSubscribedSubscriber>>();

        Test.Run(async () =>
        {
            sbr.Status.Should().Be(ServiceStatus.Initializing);
            sbr.StatusReason.Should().BeNull();

            await sbr.StartAsync().ConfigureAwait(false);
            sbr.Status.Should().Be(ServiceStatus.Running);
            sbr.StatusReason.Should().BeNull();

            await sbr.PauseAsync("Reason").ConfigureAwait(false);
            sbr.Status.Should().Be(ServiceStatus.Paused);
            sbr.StatusReason.Should().Be("Reason");

            await sbr.ResumeAsync().ConfigureAwait(false);
            sbr.Status.Should().Be(ServiceStatus.Running);
            sbr.StatusReason.Should().BeNull();

            await sbr.StopAsync().ConfigureAwait(false);
            sbr.Status.Should().Be(ServiceStatus.Stopped);
            sbr.StatusReason.Should().BeNull();
        }).AssertSuccess();
    });

    [Test]
    public void ReceiveAsync_Success() => Test.ScopedType<ExecutionContext>(async test =>
    {
        // Publish a message.
        var sp = (ServiceBusPublisher)test.Services.GetRequiredKeyedService<IEventPublisher>(ServiceBusPublisher.DefaultServiceKey);
        sp.Add(EventData.CreateEventWith(new Subscribers.Product { Id = 1, Sku = "SKU-001" }, "Created"));
        await sp.PublishAsync();

        // Create using the root services (not scoped).
        var o = ServiceBusReceiverOptions.CreateForTopicSubscription("unit-test", "default");
        var sbr = ActivatorUtilities.CreateInstance<ServiceBusReceiver<ServiceBusSubscribedSubscriber>>(Test.Services, o);

        var cts = new CancellationTokenSource();
        cts.CancelAfter(10000); // Ensure test doesn't run indefinitely.
        sbr.MessageProcessed += (sender, e) => cts.CancelAfter(5); // Cancel shortly after processing to allow for graceful completion.

        // Act and assert.
        Test.ExpectLogContains("Received product with Id: 1 and Sku: SKU-001.")
            .Run(async () =>
            {
                try
                {
                    await sbr.StartAsync(cts.Token).ConfigureAwait(false);
                    await Task.Delay(Timeout.Infinite, cts.Token); // Wait for the message to be processed or timeout; then stop and dispose.
                }
                finally
                {
                    await sbr.StopAsync().ConfigureAwait(false);
                    await sbr.DisposeAsync().ConfigureAwait(false);
                }
            }).AssertException<TaskCanceledException>();
    });

    [Test]
    public void StopAsync_NeverStarted_DoesNotThrow() => Test.ScopedType<ExecutionContext>(async test =>
    {
        // Regression: StopAsync must not invoke OnStopAsync (i.e. must not attempt to stop the underlying processor) for a receiver that was never started.
        var o = ServiceBusReceiverOptions.CreateForTopicSubscription("unit-test", "default");
        var sbr = ActivatorUtilities.CreateInstance<ServiceBusReceiver<ServiceBusSubscribedSubscriber>>(Test.Services, o);

        sbr.Status.Should().Be(ServiceStatus.Initializing);

        await sbr.StopAsync().ConfigureAwait(false);
        sbr.Status.Should().Be(ServiceStatus.Stopped);

        await sbr.DisposeAsync().ConfigureAwait(false);
    });

    [Test]
    public void WithKeyedSubscriber_WithHostedService_ResolvesReceiver() => Test.ScopedType<ExecutionContext>(async test =>
    {
        // Regression: GetReceiverInstance previously looked up the receiver using the *subscriber's* service key (the one passed to WithKeyedSubscriber) rather than the *receiver's own* service key (what
        // Build() actually registers the receiver under) - so chaining WithKeyedSubscriber(...).WithHostedService() would throw resolving a keyed receiver that was, in fact, registered non-keyed.
        var services = new ServiceCollection();
        services.AddSingleton(test.Services.GetRequiredService<ServiceBusClient>());
        services.AddSingleton(test.Services.GetRequiredService<IConfiguration>());
        services.AddLogging();

        services.AzureServiceBusReceiving()
            .WithReceiver(_ => ServiceBusReceiverOptions.CreateForTopicSubscription("unit-test", "default"))
            .WithKeyedSubscriber<ServiceBusSubscribedSubscriber>("test-subscriber-key")
            .WithHostedService("test-hosted-service-key")
            .Build();

        await using var sp = services.BuildServiceProvider();

        // Resolving IHostedService triggers the keyed HostedServiceBase factory, which in turn calls GetReceiverInstance; previously this threw
        // InvalidOperationException ("No service for type ... key 'test-subscriber-key' has been registered") because the receiver was actually
        // registered non-keyed. AddHealthChecks() also contributes its own unrelated HealthCheckPublisherHostedService, hence the OfType filter.
        var hostedServices = sp.GetServices<IHostedService>().ToList();
        hostedServices.OfType<ServiceBusReceiverHostedService<ServiceBusReceiver<ServiceBusSubscribedSubscriber>>>().Should().ContainSingle();
    });

    [Test]
    public void ReceiveAsync_OwnTokenCancellation_DoesNotLogAsUnhandled() => Test.ScopedType<ExecutionContext>(async test =>
    {
        // Regression: a cancellation attributable to the receiver's own cancellationToken (simulating a host/processor
        // shutdown while a message is in flight) must not be logged as "An unhandled error has occurred" and must not
        // throw attempting to abandon the message with the already-cancelled token.
        var sp = (ServiceBusPublisher)test.Services.GetRequiredKeyedService<IEventPublisher>(ServiceBusPublisher.DefaultServiceKey);
        sp.Add(EventData.CreateEventWith(new Subscribers.Product { Id = 200, Sku = "SKU-200" }, "Created"));
        await sp.PublishAsync();

        var o = ServiceBusReceiverOptions.CreateForTopicSubscription("unit-test", "default");
        var sbr = ActivatorUtilities.CreateInstance<ServiceBusReceiver<ServiceBusSubscribedSubscriber>>(Test.Services, o);

        var cts = new CancellationTokenSource();
        cts.CancelAfter(10000); // Ensure test doesn't run indefinitely.
        sbr.MessageProcessed += (sender, e) => { }; // No-op: Id 200 blocks on the token itself and is only "processed" once cancellation propagates.

        var assertor = Test.ExpectLogContains("Received product with Id: 200 and Sku: SKU-200.")
            .Run(async () =>
            {
                try
                {
                    await sbr.StartAsync(cts.Token).ConfigureAwait(false);
                    await Task.Delay(200, cts.Token).ConfigureAwait(false); // Allow the message to be received and start processing (blocking on cts.Token).
                    cts.Cancel(); // Simulate host/processor shutdown while the message is in flight.
                    await Task.Delay(Timeout.Infinite, cts.Token).ConfigureAwait(false);
                }
                finally
                {
                    await sbr.StopAsync().ConfigureAwait(false);
                    await sbr.DisposeAsync().ConfigureAwait(false);
                }
            }).AssertException<TaskCanceledException>();

        assertor.LogMessages.Any(x => x?.Contains("An unhandled error has occurred") == true).Should().BeFalse();
    });

    [Test]
    public void ReceiveAsync_Retry_Then_DeadLetter() => Test.ScopedType<ExecutionContext>(async test =>
    {
        // Publish a message.
        var sp = (ServiceBusPublisher)test.Services.GetRequiredKeyedService<IEventPublisher>(ServiceBusPublisher.DefaultServiceKey);
        sp.Add(EventData.CreateEventWith(new Subscribers.Product { Id = 88, Sku = "SKU-088" }, "Created"));
        await sp.PublishAsync();

        // Create using the root services (not scoped).
        var o = ServiceBusReceiverOptions.CreateForTopicSubscription("unit-test", "default");
        o.RetryErrorHandling = Events.Subscribing.ErrorHandling.DeadLetter;
        o.MessageResiliency = ServiceBusReceiverResiliency.CreateMessageRetryResiliency(TimeSpan.FromMilliseconds(333), 3, Polly.DelayBackoffType.Exponential);
        o.PerUnhandledErrorDelayDuration = TimeSpan.FromMilliseconds(100);

        var sbr = ActivatorUtilities.CreateInstance<ServiceBusReceiver<ServiceBusSubscribedSubscriber>>(Test.Services, o);

        var cts = new CancellationTokenSource();
        cts.CancelAfter(10000); // Ensure test doesn't run indefinitely.
        sbr.MessageProcessed += (sender, e) => cts.CancelAfter(10); // Cancel shortly after processing to allow for graceful completion.

        // Act and assert.
        Test.ExpectLogContains("Received product with Id: 88 and Sku: SKU-088.")
            .ExpectLogContains("A transient error has occurred; please try again. [Source: ServiceBusSubscribedSubscriber, Handling: Retry]")
            .ExpectLogContains("Retry attempt 1 in 333ms.")
            .ExpectLogContains("Retry attempt 2 in 666ms.")
            .ExpectLogContains("Retry attempt 3 in 1332ms.")
            .ExpectLogContains("DeadLetterAsync")
            .Run(async () =>
            {
                try
                {
                    await sbr.StartAsync(cts.Token).ConfigureAwait(false);
                    await Task.Delay(Timeout.Infinite, cts.Token); // Wait for the message to be processed or timeout; then stop and dispose.
                }
                finally
                {
                    await sbr.StopAsync().ConfigureAwait(false);
                    await sbr.DisposeAsync().ConfigureAwait(false);
                }
            }).AssertException<TaskCanceledException>();
    });

    [Test]
    public void ReceiveAsync_Catastrophic_Then_Pause() => Test.ScopedType<ExecutionContext>(async test =>
    {
        // Publish a message.
        var sp = (ServiceBusPublisher)test.Services.GetRequiredKeyedService<IEventPublisher>(ServiceBusPublisher.DefaultServiceKey);
        sp.Add(EventData.CreateEventWith(new Subscribers.Product { Id = 99, Sku = "SKU-099" }, "Created"));
        await sp.PublishAsync();

        // Create using the root services (not scoped).
        var o = ServiceBusReceiverOptions.CreateForTopicSubscription("unit-test", "default");
        o.ReceiverResiliency = ServiceBusReceiverResiliency.CreateReceiverCircuitBreakerResiliency(5, TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(333));
        o.PerUnhandledErrorDelayDuration = TimeSpan.FromMilliseconds(100);

        var sbr = ActivatorUtilities.CreateInstance<ServiceBusReceiver<ServiceBusSubscribedSubscriber>>(Test.Services, o);

        var cts = new CancellationTokenSource();
        cts.CancelAfter(5000); // Ensure test doesn't run indefinitely.

        int messagesProcessed = 0;

        sbr.MessageProcessed += (sender, e) =>
        {
            messagesProcessed++;
            System.Threading.Thread.Sleep(10); // Allow some time to catch up.
            sbr.Status.Should().BeOneOf(ServiceStatus.Pausing, ServiceStatus.Paused);
            sbr.StatusReason.Should().Be("A Catastrophic error occurred within the service bus receiver.");
            cts.Cancel();
        };

        // Act and assert.
        var assertor = Test.ExpectLogContains("Received product with Id: 99 and Sku: SKU-099.")
            .ExpectLogContains("A Catastrophic error has occurred within the service bus receiver for subscriber 'ServiceBusSubscribedSubscriber'. Abandoning the message and pausing the receiver.")
            .ExpectLogContains("AbandonAsync done.")
            .ExpectLogContains("Azure Service Bus receiver: Pausing.")
            .ExpectLogContains("Azure Service Bus receiver: Paused.")
            .Run(async () =>
            {
                try
                {
                    await sbr.StartAsync(cts.Token).ConfigureAwait(false);
                    await Task.Delay(Timeout.Infinite, cts.Token); // Wait for the message to be processed or timeout; then stop and dispose.
                }
                finally
                {
                    await sbr.StopAsync().ConfigureAwait(false);
                    await sbr.DisposeAsync().ConfigureAwait(false);
                }
            }).AssertException<TaskCanceledException>();

        messagesProcessed.Should().BeGreaterThanOrEqualTo(1);
    });

    [Test]
    public void ReceiveAsync_CircuitBreaker()
    {
        for (int i = 0; i < 2; i++)
        {
            // If successful, break out of the loop; otherwise, retry once.
            if (ReceiveAsync_CircuitBreaker_Internal())
                break; 

            if (i > 0)
               Assert.Fail("The circuit breaker did not trip as expected. This has been attempted twice.");
        }
    }

    private bool ReceiveAsync_CircuitBreaker_Internal()
    {
        bool circuitBreakerTripped = false;

        Test.ScopedType<ExecutionContext>(async test =>
        {
            await ReceiveAllMessages();

            // Publish a message.
            var sp = (ServiceBusPublisher)test.Services.GetRequiredKeyedService<IEventPublisher>(ServiceBusPublisher.DefaultServiceKey);
            sp.Add(EventData.CreateEventWith(new Subscribers.Product { Id = 109, Sku = "SKU-109" }, "Created"));
            sp.Add(EventData.CreateEventWith(new Subscribers.Product { Id = 109, Sku = "SKU-109.1" }, "Created"));
            sp.Add(EventData.CreateEventWith(new Subscribers.Product { Id = 109, Sku = "SKU-109.2" }, "Created"));
            await sp.PublishAsync();

            // Create using the root services (not scoped).
            var o = ServiceBusReceiverOptions.CreateForTopicSubscription("unit-test", "default");
            o.ReceiverResiliency = ServiceBusReceiverResiliency.CreateReceiverCircuitBreakerResiliency(5, TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(333));
            o.PerUnhandledErrorDelayDuration = TimeSpan.FromMilliseconds(100);

            var sbr = ActivatorUtilities.CreateInstance<ServiceBusReceiver<ServiceBusSubscribedSubscriber>>(Test.Services, o);

            var count = 0;
            sbr.MessageProcessed += (sender, e) => count++;

            var cts = new CancellationTokenSource();
            cts.CancelAfter(30000); // Ensure test doesn't run indefinitely.

            var assertor = Test.Run(async () =>
            {
                try
                {
                    await sbr.StartAsync(cts.Token).ConfigureAwait(false);
                    await Task.Delay(Timeout.Infinite, cts.Token); // Wait for the message to be processed or timeout; then stop and dispose.
                }
                finally
                {
                    await sbr.StopAsync().ConfigureAwait(false);
                    await sbr.DisposeAsync().ConfigureAwait(false);

                    if (Test.Logger.IsEnabled(LogLevel.Information))
                        Test.Logger.LogInformation("MESSAGE PROCESSED COUNT: {Count}.", count);
                }
            }).AssertException<TaskCanceledException>();

            circuitBreakerTripped = assertor.LogMessages.Any(x => x?.Contains("Service bus receiver circuit breaker has been tripped for 333ms due to unhandled errors; will be paused.") == true)
                && assertor.LogMessages.Any(x => x?.Contains("Service bus receiver circuit breaker has been tripped for 666ms due to unhandled errors; will be paused.") == true)
                && assertor.LogMessages.Any(x => x?.Contains("Service bus receiver circuit breaker has been tripped for 1332ms due to unhandled errors; will be paused.") == true)
                && assertor.LogMessages.Any(x => x?.Contains("Service bus receiver circuit breaker is attempting to recover in a limited state; has been resumed.") == true);
        });

        return circuitBreakerTripped;
    }
}