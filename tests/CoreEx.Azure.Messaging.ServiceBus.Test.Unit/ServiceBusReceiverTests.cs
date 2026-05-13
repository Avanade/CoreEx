using Azure.Messaging.ServiceBus;
using CoreEx.Events;
using CoreEx.Events.Publishing;
using CoreEx.Hosting;
using Microsoft.Extensions.DependencyInjection;
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
        while (true)
        {
            var messages = await receiver.ReceiveMessagesAsync(maxMessages: 50, maxWaitTime: TimeSpan.FromMilliseconds(5));
            if (messages.Count == 0)
                break;

            foreach (var m in messages)
                await receiver.CompleteMessageAsync(m);
        }
    }

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
            .ExpectLogContains("Service bus message retry attempt 1 in 333ms.")
            .ExpectLogContains("Service bus message retry attempt 2 in 666ms.")
            .ExpectLogContains("Service bus message retry attempt 3 in 1332ms.")
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
        Test.ExpectLogContains("Received product with Id: 99 and Sku: SKU-099.")
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
            try
            {
                ReceiveAsync_CircuitBreaker_Internal();
                break; // If successful, break out of the loop; otherwise, if an exception occurs on the first run, it will be caught and retried once more.
            }
            catch (Exception)
            {
                if (i > 0)
                    throw;

                // NUnit records assertion failures into its TestExecutionContext before throwing, so even though the exception is caught above, the first-pass failure is already registered.
                // Explicitly reset the test result here to clear any recorded failures so the retry attempt is treated as a clean run.
                NUnit.Framework.Internal.TestExecutionContext.CurrentContext.CurrentResult
                    .SetResult(NUnit.Framework.Interfaces.ResultState.Success);
            }
        }
    }

    private void ReceiveAsync_CircuitBreaker_Internal() => Test.ScopedType<ExecutionContext>(async test =>
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

        Test.ExpectLogContains("Service bus receiver circuit breaker has been tripped for 333ms due to unhandled errors; receiver will be paused.")
            .ExpectLogContains("Service bus receiver circuit breaker has been tripped for 666ms due to unhandled errors; receiver will be paused.")
            .ExpectLogContains("Service bus receiver circuit breaker has been tripped for 1332ms due to unhandled errors; receiver will be paused.")
            .ExpectLogContains("Service bus receiver circuit breaker is attempting to recover in a limited state; receiver has been resumed.")
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

                    if (Test.Logger.IsEnabled(LogLevel.Information))
                        Test.Logger.LogInformation("MESSAGE PROCESSED COUNT: {Count}.", count);
                }
            }).AssertException<TaskCanceledException>();
    });
}