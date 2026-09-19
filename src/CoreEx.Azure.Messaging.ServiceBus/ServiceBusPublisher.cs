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
    /// <summary>
    /// The default <see cref="SendResiliency"/>, built once and shared across every instance - since <see cref="ServiceBusPublisher"/> is typically registered scoped (a new instance per DI scope),
    /// building a fresh <see cref="ResiliencePipeline{T}"/> per instance would be wasted, avoidable allocation on what can be a busy path.
    /// </summary>
    private static readonly ResiliencePipeline<Result> DefaultSendResiliency = ServiceBusPublisherResiliency.CreateSendRetryResiliency();

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

    /// <summary>
    /// Gets the default <see cref="NoPartitionKeySessionId"/> value: '<c>$none</c>'.
    /// </summary>
    public const string DefaultNoPartitionKeySessionId = "$none";

    /// <summary>
    /// Gets or sets the fixed value used to derive the <see cref="ServiceBusMessage.SessionId"/> for an event with no <see cref="EventData.PartitionKey"/>, regardless of <see cref="SessionIdStrategy"/>;
    /// defaults to <see cref="DefaultNoPartitionKeySessionId"/>.
    /// </summary>
    /// <remarks>Used directly as the <see cref="ServiceBusMessage.SessionId"/> where <see cref="SessionIdStrategy"/> is <see cref="ServiceBusSessionStrategy.UsePartitionKeyAsIs"/>, or as the hash input to
    /// <see cref="Data.PartitionKey.GetPartitionIdAsString"/> where <see cref="SessionIdStrategy"/> is <see cref="ServiceBusSessionStrategy.UsePartitionKeyConvertedToAnId"/> (in which case it consistently lands
    /// in the same one of the configured pool of sessions, rather than being spread across the pool).
    /// <para>Using a single fixed value (rather than a new random value per message) for <i>both</i> strategies means partition-key-less events are always funnelled through the same session/bucket, and therefore
    /// preserve their relative publish order - a random value per message, by contrast, would give no ordering guarantee between two such events at all, even though "no key" does not imply "order doesn't matter".</para>
    /// <para>The tradeoff: all partition-key-less events share (at most) one session and so serialize through it. If partition-key-less events are expected to represent a significant proportion of overall
    /// throughput, that concentration may become a bottleneck (or, for <see cref="ServiceBusSessionStrategy.UsePartitionKeyConvertedToAnId"/>, a hot bucket) - in which case those events should generally be given
    /// a real partition key rather than relying on this fallback.</para></remarks>
    public string NoPartitionKeySessionId { get; set; } = DefaultNoPartitionKeySessionId;

    /// <summary>
    /// Gets or sets the <see cref="ResiliencePipeline{T}"/> applied around each send within <see cref="SendBatchAsync"/>; defaults to <see cref="ServiceBusPublisherResiliency.CreateSendRetryResiliency"/>.
    /// </summary>
    /// <remarks>Consider using <see cref="ServiceBusPublisherResiliency.CreateSendRetryResiliency(TimeSpan?, int, DelayBackoffType)"/> to adjust the retry timing/attempts while keeping the same
    /// transient-failure classification, rather than constructing a pipeline from scratch.</remarks>
    public ResiliencePipeline<Result> SendResiliency { get; set => field = value.ThrowIfNull(); } = DefaultSendResiliency;

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
                    // Transient send failures (throttling, a momentary service timeout, etc.) are retried silently within this pipeline; only a genuinely sustained/permanent failure propagates.
                    var ctx = ResilienceContextPool.Shared.Get(cancellationToken);
                    try
                    {
                        ctx.Properties.Set(ResilienceOwner<ServiceBusPublisher>.PropertyKey, this);

                        var result = await SendResiliency.ExecuteAsync(static async (rc, state) =>
                        {
                            try
                            {
                                await state.sender.SendMessagesAsync(state.batch, rc.CancellationToken).ConfigureAwait(false);
                                return Result.Success;
                            }
                            catch (Exception ex)
                            {
                                return Result.Fail(ex);
                            }
                        }, ctx, (sender, batch)).ConfigureAwait(false);

                        result.ThrowOnError();
                    }
                    finally
                    {
                        ResilienceContextPool.Shared.Return(ctx);
                    }

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
            ServiceBusSessionStrategy.UsePartitionKeyAsIs => message.Adjust(message => message.SessionId = message.PartitionKey ?? LogNoPartitionKeyFallback(message, NoPartitionKeySessionId)),
            ServiceBusSessionStrategy.UsePartitionKeyConvertedToAnId => message.Adjust(message => message.SessionId = message.PartitionKey = PartitionKey.GetPartitionIdAsString(message.PartitionKey ?? LogNoPartitionKeyFallback(message, NoPartitionKeySessionId), SessionIdPartitionSize ?? PartitionKey.DefaultPartitionSize)),
            _ => message
        };
    }

    /// <summary>
    /// Logs (at <see cref="LogLevel.Debug"/>) that an event with no <see cref="EventData.PartitionKey"/> is falling back to the specified <paramref name="fallbackValue"/> for session assignment.
    /// </summary>
    private string LogNoPartitionKeyFallback(ServiceBusMessage message, string fallbackValue)
    {
        if (Logger?.IsEnabled(LogLevel.Debug) ?? false)
            Logger.LogDebug("Event (Id={MessageId}) has no PartitionKey; falling back to '{FallbackValue}' for {SessionIdStrategy} session assignment.", message.MessageId, fallbackValue, SessionIdStrategy);

        return fallbackValue;
    }
}
