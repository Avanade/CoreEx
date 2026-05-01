namespace CoreEx.Azure.Messaging.ServiceBus;

/// <summary>
/// Provides the <see cref="IEventPublisher"/> implementation for <see href="https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-messaging-overview">Azure Service Bus</see>.
/// </summary>
/// <param name="serviceBusClient">The <see cref="ServiceBusClient"/>.</param>
/// <param name="destinationProvider">The optional <see cref="IDestinationProvider"/>.</param>
/// <param name="formatter">The optional <see cref="IEventFormatter"/>.</param>
/// <param name="logger">The optional logger.</param>
/// <remarks>Sends using <see href="https://github.com/Azure/azure-sdk-for-net/tree/main/sdk/servicebus/Azure.Messaging.ServiceBus#sending-a-batch-of-messages">safe-batching</see>.
/// <para>This implementation enables at-least once delivery; i.e. there are no guarantees that events are not delivered more than once where an underlying <see cref="Exception"/> is thrown.</para>
/// <para>Where <see href="https://learn.microsoft.com/en-us/azure/service-bus-messaging/message-sessions"></see> are required then the <see cref="ServiceBusSessionStrategy"/> must be configured accordingly.</para></remarks>
public sealed class ServiceBusPublisher(ServiceBusClient serviceBusClient, IDestinationProvider? destinationProvider = null, IEventFormatter? formatter = null, ILogger<ServiceBusPublisher>? logger = null) : EventPublisherBase(destinationProvider, formatter, logger)
{
    private readonly ServiceBusClient _serviceBusClient = serviceBusClient.ThrowIfNull();

    /// <summary>
    /// Gets the default service key used when registering the service.
    /// </summary>
    /// <remarks>See related <see cref="CoreExServiceBusExtensions.AddAzureServiceBusPublisher"/>.</remarks>
    public const string DefaultServiceKey = "AzureServiceBus";

    /// <summary>
    /// Gets or sets the <see cref="CloudNative.CloudEvents.ContentMode"/> to use when sending a <see cref="CloudEvent"/> as a <see cref="ServiceBusMessage"/>; defaults to <see cref="ContentMode.Structured"/>.
    /// </summary>
    /// <remarks>See also <see cref="ServiceBusExtensions.ToServiceBusMessage"/>.</remarks>
    public ContentMode ContentMode { get; set; } = ContentMode.Structured;

    /// <summary>
    /// Indicates whether to include all <see cref="CloudEvent.GetPopulatedAttributes"/> as <see cref="ServiceBusMessage.ApplicationProperties"/>; defaults to <see langword="true"/>.
    /// </summary>
    /// <remarks>See also <see cref="ServiceBusExtensions.ToServiceBusMessage"/>.</remarks>
    public bool IncludeCloudEventAttributes { get; set; } = true;

    /// <summary>
    /// Gets or sets the <see cref="ServiceBusMessage.SessionId"/> strategy to use when sending messages; defaults to <see cref="ServiceBusSessionStrategy.None"/>.
    /// </summary>
    public ServiceBusSessionStrategy SessionIdStrategy { get; set; } = ServiceBusSessionStrategy.None;

    /// <summary>
    /// Gets or sets the size of the partition used for when the <see cref="SessionIdStrategy"/> is <see cref="ServiceBusSessionStrategy.UsePartitionKeyConvertedToAnId"/>.
    /// </summary>
    /// <remarks>Where not specified the <see cref="PartitionKey.DefaultPartitionSize"/> is used.</remarks>
    public int? SessionIdPartitionSize { get; set; }

    /// <inheritdoc/>
    protected async override Task OnPublishAsync(DestinationEvent[] events, CancellationToken cancellationToken = default)
    {
        var groups = events
            .GroupBy(e => e.Destination ?? string.Empty, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => new Queue<DestinationEvent>(g), StringComparer.Ordinal);

        if (Logger?.IsEnabled(LogLevel.Debug) ?? false)
            Logger.LogDebug("Preparing to send {EventCount} event(s) to {DestinationCount} destination(s).", events.Length, groups.Count);

        foreach (var group in groups)
        {
            await SendBatchAsync(events.Length, group.Key, group.Value, cancellationToken).ConfigureAwait(false);
        }

        if (Logger?.IsEnabled(LogLevel.Debug) ?? false)
            Logger.LogDebug("Published {Count} event(s) to Azure Service Bus.", events.Length);
    }

    /// <summary>
    /// Send using safe-batching.
    /// </summary>
    private async Task SendBatchAsync(int totalEventCount, string destination, Queue<DestinationEvent> events, CancellationToken cancellationToken)
    {
        var eventsSent = 0;

        // Create a sender for the queue/topic (destination).
        await using var sender = _serviceBusClient.CreateSender(destination);

        while (events.Count > 0)
        {
            // Start a new batch.
            using var batch = await sender.CreateMessageBatchAsync(cancellationToken).ConfigureAwait(false);

            // Add first message to the batch.
            if (batch.TryAddMessage(SetSessionId(events.Peek().Event.ToServiceBusMessage(ContentMode, IncludeCloudEventAttributes))))
            {
                events.Dequeue();

                // Keep adding messages until we run out of messages or batch is full.
                while (events.Count > 0 && batch.TryAddMessage(SetSessionId(events.Peek().Event.ToServiceBusMessage(ContentMode, IncludeCloudEventAttributes))))
                {
                    events.Dequeue();
                }
            }
            else
            {
                if (Logger?.IsEnabled(LogLevel.Error) ?? false)
                {
                    var ce = events.Peek().Event;
                    Logger.LogError("A single event (Id={MessageId}, Type='{MessageType}') is too large to fit in the Azure Service Bus message batch for destination '{Destination}'; {EventsSent} of the {EventsCount} event(s) have already been successfully sent.", ce.Id, ce.Type, destination, eventsSent, totalEventCount);
                }

                throw new InvalidOperationException("A single event is too large to fit in the Azure Service Bus message batch.");
            }

            // Send the batch of messages.
            if (Logger?.IsEnabled(LogLevel.Debug) ?? false)
                Logger.LogDebug("Sending batch of {BatchCount} event(s) to destination '{Destination}'.", batch.Count, destination);

            await Invoker.InvokeAsync(this, async (tracer, cancellationToken) =>
            {
                tracer.Activity?.AddTag("servicebus.destination", destination);
                var stopwatch = Stopwatch.StartNew();

                try
                {
                    await sender.SendMessagesAsync(batch, cancellationToken).ConfigureAwait(false);
                    ServiceBusMetrics.MessagesSendSent.Add(batch.Count, [ new (ServiceBusMetrics.DestinationTagName, destination) ]);
                }
                catch (Exception)
                {
                    ServiceBusMetrics.MessagesSendFailed.Add(batch.Count, [new(ServiceBusMetrics.DestinationTagName, destination)]);
                    throw;
                }
                finally
                {
                    stopwatch.Stop();
                    ServiceBus.ServiceBusMetrics.MessagesSendDuration.Record(stopwatch.Elapsed.TotalMilliseconds, [new(ServiceBusMetrics.DestinationTagName, destination)]);
                }

                tracer.Activity?.AddTag("servicebus.messages.sent", batch.Count);
            }, cancellationToken).ConfigureAwait(false);

            eventsSent += batch.Count;
        }
    }

    /// <summary>
    /// Sets the <see cref="ServiceBusMessage"/> <see cref="ServiceBusMessage.SessionId"/> based on the configured <see cref="SessionIdStrategy"/>.
    /// </summary>
    private ServiceBusMessage SetSessionId(ServiceBusMessage message)
    {
        message.ThrowIfNull();

        return SessionIdStrategy switch
        {
            ServiceBusSessionStrategy.UsePartitionKeyAsIs => message.Adjust(message => message.SessionId = message.PartitionKey ?? Guid.NewGuid().ToString()),
            ServiceBusSessionStrategy.UsePartitionKeyConvertedToAnId => message.Adjust(message => message.SessionId = message.PartitionKey = PartitionKey.GetPartitionIdAsString(message.PartitionKey ?? Guid.NewGuid().ToString(), SessionIdPartitionSize ?? PartitionKey.DefaultPartitionSize)),
            _ => message
        };
    }
}