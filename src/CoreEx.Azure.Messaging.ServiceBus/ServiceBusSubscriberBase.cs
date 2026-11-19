namespace CoreEx.Azure.Messaging.ServiceBus;

/// <summary>
/// Provides the base <see href="https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-messaging-overview">Azure Service Bus</see> subscribing capabilities.
/// </summary>
/// <param name="formatter">The <see cref="IEventFormatter"/>.</param>
/// <param name="logger">The <see cref="ILogger{ServiceBusSubscriber}"/>.</param>
public abstract class ServiceBusSubscriberBase(IEventFormatter formatter, ILogger<ServiceBusSubscriberBase> logger) : EventSubscriberBase(formatter, logger)
{
    private static readonly IEventFormatter _defaultFormatter = new EventFormatter();

    /// <summary>
    /// Initializes subscriber infrastructure using the shared default formatter.
    /// </summary>
    /// <param name="logger">The subscriber logger.</param>
    /// <remarks>Native message processing never invokes the formatter. Do not mutate the shared formatter; supply a configured formatter using the other constructor when customization is required.</remarks>
    protected ServiceBusSubscriberBase(ILogger<ServiceBusSubscriberBase> logger) : this(_defaultFormatter, logger) { }

    /// <summary>
    /// Gets or sets the expected message representation; defaults to <see cref="MessageRepresentation.CloudEvent"/>.
    /// </summary>
    /// <remarks>CloudEvent mode requires an envelope, uses formatter-resolved correlation, and never falls back to native JSON. Native mode uses the broker correlation identifier and requires a typed subscriber or an override of <see cref="OnReceiveMessageAsync"/>.</remarks>
    /// <exception cref="InvalidOperationException">Native mode was selected on a subscriber that declares <see cref="SubscribeAttribute"/>.</exception>
    public MessageRepresentation DataRepresentation
    {
        get;
        set
        {
            if (!Enum.IsDefined(value))
                throw new ArgumentOutOfRangeException(nameof(value), "The message representation is not supported.");

            if (value == MessageRepresentation.Native && HasSubscriptions)
                throw new InvalidOperationException("A Service Bus subscriber declaring [Subscribe] requires CloudEvent processing.");

            field = value;
        }
    } = MessageRepresentation.CloudEvent;

    /// <summary>
    /// Gets or sets the <see cref="CloudNative.CloudEvents.ContentMode"/> to use when receiving <see cref="CloudEvent"/> messages.
    /// </summary>
    /// <remarks>Where <see langword="null"/> (the default), then the <see cref="CloudNative.CloudEvents.ContentMode"/> will be inferred from the <see cref="ServiceBusReceivedMessage.ContentType"/> (see <see cref="EventsExtensions.InferContentMode"/>);
    /// this offers the greatest flexibility and is the recommended value.</remarks>
    public ContentMode? ContentMode { get; set; }

    /// <summary>
    /// Receives a <see cref="ServiceBusReceivedMessage"/>.
    /// </summary>
    /// <param name="message">The <see cref="ServiceBusReceivedMessage"/>.</param>
    /// <param name="args">The optional <see cref="EventSubscriberArgs"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    public async Task<Result> ReceiveAsync(ServiceBusReceivedMessage message, EventSubscriberArgs? args = null, CancellationToken cancellationToken = default)
    {
        args ??= new EventSubscriberArgs();
        args.Owner ??= this;
        args.Message = message.ThrowIfNull();

        return await InvokeReceiveAttemptAsync(args, async c =>
        {
            message.EnrichReceiveActivity(PropagateBaggage);

            using var receiveScope = DataRepresentation == MessageRepresentation.Native ? Correlation.BeginReceive(message.CorrelationId, Logger) : null;

            return await EventSubscriberMetrics.ReceiveMessageAsync(args, async () =>
            {
                // Pre-process.
                var br = await OnBeforeReceiveAsync(message, args, c).ConfigureAwait(false);
                if (br.IsFailure)
                    return br;

                return DataRepresentation == MessageRepresentation.Native
                    ? await InvokeBehaviorsAsync(args, null, true, ct => OnReceiveMessageAsync(message, args, ct), c).ConfigureAwait(false)
                    : await OnReceiveMessageAsync(message, args, c).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Processes the message, converting to a CloudEvent by default.
    /// </summary>
    /// <param name="message">The received message.</param>
    /// <param name="args">The subscriber arguments.</param>
    /// <param name="cancellationToken">The receive cancellation token.</param>
    /// <returns>The processing result.</returns>
    /// <remarks>Passes the decoded CloudEvent to the shared receive path, which checks declared subscriptions before formatter conversion. Overrides should use that path to retain the check.</remarks>
    protected virtual Task<Result> OnReceiveMessageAsync(ServiceBusReceivedMessage message, EventSubscriberArgs args, CancellationToken cancellationToken = default)
        => ReceiveWrapperAsync(args, async () =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (DataRepresentation != MessageRepresentation.CloudEvent)
                throw new NotSupportedException("Native processing requires a typed Service Bus subscriber or an override of OnReceiveMessageAsync.");

            CloudEvent cloudEvent;
            try
            {
                cloudEvent = message.ToCloudEvent(ContentMode);
                cloudEvent.Validate();
            }
            catch (Exception ex) when (ex is JsonException or FormatException or ArgumentException or InvalidOperationException or NotSupportedException)
            {
                return InvalidDataResult<object>("An error occurred in the event subscriber during CloudEvent envelope decoding.", ex).AsResult();
            }

            return await ReceiveAsync(cloudEvent, args, cancellationToken).ConfigureAwait(false);
        }, cancellationToken);

    /// <summary>
    /// Receives the <see cref="ServiceBusReceivedMessage"/> providing an opportunity to perform actions before CloudEvent or native message processing.
    /// </summary>
    /// <param name="message">The <see cref="ServiceBusReceivedMessage"/>.</param>
    /// <param name="args">The <see cref="EventSubscriberArgs"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="Result"/>.</returns>
    protected virtual Task<Result> OnBeforeReceiveAsync(ServiceBusReceivedMessage message, EventSubscriberArgs args, CancellationToken cancellationToken = default) => Result.SuccessTask;
}
