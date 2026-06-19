#pragma warning disable IDE0130 // Namespace does not match folder structure; by design.
namespace UnitTestEx;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public static partial class UnitTestExExtensions
{
    /// <summary>
    /// Gets all messages for the Azure Service Bus queue or topic subscription completing each resulting in all messages also being cleared.
    /// </summary>
    /// <param name="tester">The <see cref="TesterBase"/>.</param>
    /// <param name="sbo">The <see cref="ServiceBusReceiverOptions"/>.</param>
    /// <returns>A list of <see cref="Asb.ServiceBusReceivedMessage"/> that were cleared.</returns>
    public static async Task<List<Asb.ServiceBusReceivedMessage>> GetAndClearAzureServiceBusAsync(this TesterBase tester, ServiceBusReceiverOptions sbo)
    {
        var sbc = tester.ThrowIfNull().Services.GetRequiredService<Asb.ServiceBusClient>();
        var qtn = CoreEx.Abstractions.Internal.GetValueFromConfigurationWhereApplicable(sbo.QueueOrTopicName, tester.Configuration);
        var list = new List<Asb.ServiceBusReceivedMessage>();

        await using var receiver = sbo.IsSubscription
            ? sbc.CreateReceiver(qtn, CoreEx.Abstractions.Internal.GetValueFromConfigurationWhereApplicable(sbo.SubscriptionName!, tester.Configuration))
            : sbc.CreateReceiver(qtn);

        while (true)
        {
            var messages = await receiver.ReceiveMessagesAsync(maxMessages: 50, maxWaitTime: TimeSpan.FromMilliseconds(1));
            if (messages.Count == 0)
                break;

            foreach (var m in messages)
                await receiver.CompleteMessageAsync(m);

            list.AddRange(messages);
        }

        return list;
    }

    /// <summary>
    /// Gets all messages for the Azure Service Bus queue or topic subscription completing each resulting in all messages also being cleaed.
    /// </summary>
    /// <param name="tester">The <see cref="TesterBase"/>.</param>
    /// <param name="sbo">The <see cref="ServiceBusSessionReceiverOptions"/>.</param>
    /// <returns>A list of <see cref="Asb.ServiceBusReceivedMessage"/> that were cleared.</returns>
    /// <remarks>This method is used for session-enabled queues or topic subscriptions.</remarks>
    public static async Task<List<Asb.ServiceBusReceivedMessage>> GetAndClearAzureServiceBusAsync(this TesterBase tester, ServiceBusSessionReceiverOptions sbo)
    {
        var sbc = tester.ThrowIfNull().Services.GetRequiredService<Asb.ServiceBusClient>();
        var qtn = CoreEx.Abstractions.Internal.GetValueFromConfigurationWhereApplicable(sbo.QueueOrTopicName, tester.Configuration);
        var list = new List<Asb.ServiceBusReceivedMessage>();

        while (true)
        {
            Asb.ServiceBusSessionReceiver? session;

            try
            {
                session = sbo.IsSubscription
                    ? await sbc.AcceptNextSessionAsync(qtn, CoreEx.Abstractions.Internal.GetValueFromConfigurationWhereApplicable(sbo.SubscriptionName!, tester.Configuration))
                    : await sbc.AcceptNextSessionAsync(qtn);
            }
            catch (Asb.ServiceBusException ex)
            {
                if (ex.Reason == Asb.ServiceBusFailureReason.ServiceTimeout || (ex.InnerException is System.Net.Sockets.SocketException innerEx && innerEx.SocketErrorCode == System.Net.Sockets.SocketError.TimedOut))
                    break; // No more sessions available

                throw;
            }

            if (session is null)
                break;

            await using (session)
            {
                while (true)
                {
                    var messages = await session.ReceiveMessagesAsync(maxMessages: 50, maxWaitTime: TimeSpan.FromMilliseconds(1));
                    if (messages.Count == 0)
                        break;

                    foreach (var msg in messages)
                        await session.CompleteMessageAsync(msg);

                    list.AddRange(messages);
                }
            }
        }

        return list;
    }

    /// <summary>
    /// Replaces the registered <see cref="IEventPublisher"/> with a decorator (<see cref="EventPublisherDecorator"/>) that also captures the published events for expectation assertions.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="serviceKey">The service key for the previously registered <see cref="IEventPublisher"/>.</param>
    /// <param name="bypassPassThrough">Indicates whether to bypass the pass-through to the original event publisher.</param>
    /// <returns>The <see cref="IServiceCollection"/> to support fluent-style method-chaining.</returns>
    /// <remarks>This is a convenience method that defaults the <paramref name="serviceKey"/> to <see cref="ServiceBusPublisher.DefaultServiceKey"/> where invoking the underlying <see cref="UseExpectedEventPublisher(IServiceCollection, string, bool)"/>.
    /// <para>The <paramref name="bypassPassThrough"/> when set to <see langword="true"/> will bypass the pass-through to the original event publisher and leverage the <see cref="NoOpEventPublisher"/> instead.</para></remarks>
    public static IServiceCollection UseExpectedAzureServiceBusPublisher(this IServiceCollection services, string serviceKey = ServiceBusPublisher.DefaultServiceKey, bool bypassPassThrough = false)
        => UseExpectedEventPublisher(services, serviceKey, bypassPassThrough);

    /// <summary>
    /// Replaces the registered <see cref="IEventPublisher"/> with a decorator (<see cref="EventPublisherDecorator"/>) that also captures the published events for expectation assertions; whilst also adding post-run expectations for the captured events.
    /// </summary>
    /// <typeparam name="TEntryPoint">The API startup <see cref="Type"/>.</typeparam>
    /// <param name="tester">The <see cref="AspNetCore.ApiTester{TEntryPoint}"/>.</param>
    /// <param name="serviceKey">The service key for the previously registered <see cref="IEventPublisher"/>.</param>
    /// <param name="bypassPassThrough">Indicates whether to bypass the pass-through to the original event publisher.</param>
    /// <param name="expectNoEvents">Indicates whether to expect no events to be published.</param>
    /// <returns>The <see cref="AspNetCore.ApiTester{TEntryPoint}"/> instance to support fluent-style method-chaining.</returns>
    /// <remarks>The <paramref name="expectNoEvents"/> parameter is only actioned when no explicit event expectations are defined for the underlying test; acts as a catch all.</remarks>
    public static AspNetCore.ApiTester<TEntryPoint> UseExpectedAzureServiceBusPublisher<TEntryPoint>(this AspNetCore.ApiTester<TEntryPoint> tester, string serviceKey = ServiceBusPublisher.DefaultServiceKey, bool bypassPassThrough = false, bool expectNoEvents = true) where TEntryPoint : class
        => tester.ConfigureServices(services => services.UseExpectedAzureServiceBusPublisher(serviceKey, bypassPassThrough))
                 .AddEventExpectationsPostRun(serviceKey, expectNoEvents);

    /// <summary>
    /// Converts a <see cref="CloudEvent"/> to a <see cref="Asb.ServiceBusReceivedMessage"/>.
    /// </summary>
    /// <param name="cloudEvent">The <see cref="CloudEvent"/>.</param>
    /// <param name="contentMode">The <see cref="ContentMode"/> to use; defaults to <see cref="ContentMode.Structured"/>.</param>
    /// <param name="includeAttributes">Indicates whether to include all <see cref="CloudEvent.GetPopulatedAttributes"/> as <see cref="Asb.ServiceBusMessage.ApplicationProperties"/>; defaults to <see langword="true"/>.</param>
    /// <returns>The <see cref="Asb.ServiceBusReceivedMessage"/>.</returns>
    /// <remarks>The <see cref="Asb.ServiceBusReceivedMessage.Subject"/> is set to the <see cref="CloudEvent.Type"/>. This converts the <see cref="CloudEvent"/> to an interim <see cref="Asb.ServiceBusMessage"/> before creating the <see cref="Asb.ServiceBusReceivedMessage"/>.</remarks>
    public static Asb.ServiceBusReceivedMessage ToServiceBusReceivedMessage(this CloudEvent cloudEvent, ContentMode contentMode = ContentMode.Structured, bool includeAttributes = true)
        => cloudEvent.ToServiceBusMessage(contentMode, includeAttributes).ToServiceBusReceivedMessage();

    /// <summary>
    /// Converts a <see cref="Asb.ServiceBusMessage"/> to a <see cref="Asb.ServiceBusReceivedMessage"/>.
    /// </summary>
    /// <param name="message">The <see cref="Asb.ServiceBusMessage"/>.</param>
    /// <returns>The <see cref="Asb.ServiceBusReceivedMessage"/>.</returns>
    public static Asb.ServiceBusReceivedMessage ToServiceBusReceivedMessage(this Asb.ServiceBusMessage message)
    {
        // Copy application properties
        var props = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        foreach (var kvp in message.ApplicationProperties)
            props[kvp.Key] = kvp.Value;

        // Create a ServiceBusReceivedMessage using the ServiceBusModelFactory with the same properties as the original message.
        return Asb.ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: message.Body,
            messageId: message.MessageId,
            partitionKey: message.PartitionKey,
            sessionId: message.SessionId,
            replyToSessionId: message.ReplyToSessionId,
            timeToLive: message.TimeToLive,
            correlationId: message.CorrelationId,
            subject: message.Subject,
            to: message.To,
            contentType: message.ContentType,
            replyTo: message.ReplyTo,
            scheduledEnqueueTime: message.ScheduledEnqueueTime,
            properties: props,
            deliveryCount: 1,
            sequenceNumber: DateTimeOffset.UtcNow.Ticks,
            enqueuedTime: DateTimeOffset.UtcNow
        );
    }
}