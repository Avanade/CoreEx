namespace CoreEx.Azure.Messaging.ServiceBus;

/// <summary>
/// Provides the base <see href="https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-messaging-overview">Azure Service Bus</see> subscribing capabilities.
/// </summary>
/// <param name="formatter">The <see cref="IEventFormatter"/>.</param>
/// <param name="logger">The <see cref="ILogger{ServiceBusSubscriber}"/>.</param>
public abstract class ServiceBusSubscriberBase(IEventFormatter formatter, ILogger<ServiceBusSubscriberBase> logger) : EventSubscriberBase(formatter, logger)
{
    private static readonly IEventFormatter _nativeFormatter = new EventFormatter();

    /// <summary>
    /// Initializes native subscriber infrastructure without requiring an unused formatter from dependency injection.
    /// </summary>
    /// <param name="logger">The subscriber logger.</param>
    /// <remarks>The shared internal formatter satisfies the inherited infrastructure dependency; native message processing never invokes it.</remarks>
    protected ServiceBusSubscriberBase(ILogger<ServiceBusSubscriberBase> logger) : this(_nativeFormatter, logger) { }

    /// <summary>
    /// Gets or sets the <see cref="CloudNative.CloudEvents.ContentMode"/> to use when receiving <see cref="CloudEvent"/> messages.
    /// </summary>
    /// <remarks>Where <see langword="null"/> (the default), then the <see cref="CloudNative.CloudEvents.ContentMode"/> will be inferred from the <see cref="ServiceBusReceivedMessage.ContentType"/> (see <see cref="EventsExtensions.InferContentMode"/>);
    /// this offers the greatest flexibility and is the recommended value.</remarks>
    public ContentMode? ContentMode { get; set; }

    /// <summary>
    /// Indicates whether correlation is sourced from the CloudEvent envelope when a message has a CloudEvent representation.
    /// </summary>
    /// <remarks>Defaults to true for subscribers that convert and process the CloudEvent envelope. Typed JSON subscribers process the broker message directly and therefore use its transport correlation instead.</remarks>
    protected virtual bool UsesCloudEventCorrelation => true;

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

        message.EnrichReceiveActivity(PropagateBaggage);

        using var receiveScope = UsesCloudEventCorrelation && message.IsCloudEvent() ? null : Correlation.BeginReceive(message.CorrelationId, Logger);

        return await EventSubscriberMetrics.ReceiveMessageAsync(args, async () =>
        {
            // Pre-process.
            var br = await OnBeforeReceiveAsync(message, args, cancellationToken).ConfigureAwait(false);
            if (br.IsFailure)
                return br;

            return await OnReceiveMessageAsync(message, args, cancellationToken).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    /// <summary>
    /// Processes the message, converting to a CloudEvent by default.
    /// </summary>
    /// <param name="message">The received message.</param>
    /// <param name="args">The subscriber arguments.</param>
    /// <param name="cancellationToken">The receive cancellation token.</param>
    /// <returns>The processing result.</returns>
    /// <remarks>Native message subscribers override this hook to bypass CloudEvent conversion.</remarks>
    protected virtual Task<Result> OnReceiveMessageAsync(ServiceBusReceivedMessage message, EventSubscriberArgs args, CancellationToken cancellationToken = default)
        => ReceiveAsync(message.ToCloudEvent(ContentMode), args, cancellationToken);

    /// <summary>
    /// Receives the <see cref="ServiceBusReceivedMessage"/> providing an opportunity to perform actions before CloudEvent or native message processing.
    /// </summary>
    /// <param name="message">The <see cref="ServiceBusReceivedMessage"/>.</param>
    /// <param name="args">The <see cref="EventSubscriberArgs"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="Result"/>.</returns>
    protected virtual Task<Result> OnBeforeReceiveAsync(ServiceBusReceivedMessage message, EventSubscriberArgs args, CancellationToken cancellationToken = default) => Result.SuccessTask;
}
