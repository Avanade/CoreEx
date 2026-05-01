namespace CoreEx.Azure.Messaging.ServiceBus;

/// <summary>
/// Provides the <see cref="SubscribedManager"/>-based <see href="https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-messaging-overview">Azure Service Bus</see> subscribing capabilities.
/// </summary>
/// <param name="subscribedManager">The <see cref="Events.Subscribing.SubscribedManager"/>.</param>
/// <param name="formatter">The <see cref="IEventFormatter"/>.</param>
/// <param name="logger">The <see cref="ILogger{ServiceBusSubscriber}"/>.</param>
/// <remarks>Leverages the <see cref="Events.Subscribing.SubscribedManager"/> to determine the appropriate <see cref="Events.Subscribing.SubscribedBase"/> for processing the received event. The <see cref="ServiceBusReceivedMessage.Subject"/>
/// is used as the <i>Title</i> and the <see cref="SourcePropertyName"/> (<see cref="ServiceBusReceivedMessage.ApplicationProperties"/>) as the <i>Source</i>.</remarks>
public sealed class ServiceBusSubscribedSubscriber(SubscribedManager subscribedManager, IEventFormatter formatter, ILogger<ServiceBusSubscriberBase> logger) : ServiceBusSubscriberBase(formatter, logger)
{
    /// <summary>
    /// Gets the <see cref="Events.Subscribing.SubscribedManager"/>.
    /// </summary>
    public SubscribedManager SubscribedManager { get; } = subscribedManager.ThrowIfNull();

    /// <summary>
    /// Gets or sets the name of the property within the <see cref="ServiceBusReceivedMessage.ApplicationProperties"/> that contains the source <see cref="Uri"/>.
    /// </summary>
    /// <remarks>Defaults to <see cref="ServiceBusExtensions.CloudEventSourcePropertyName"/>.</remarks>
    public string SourcePropertyName { get; set; } = ServiceBusExtensions.CloudEventSourcePropertyName;

    /// <inheritdoc/>
    protected override Task<Result> OnBeforeReceiveAsync(ServiceBusReceivedMessage message, EventSubscriberArgs args, CancellationToken cancellationToken = default)
    {
        if (Logger.IsEnabled(LogLevel.Debug))
            Logger.LogDebug("Received ServiceBusMessage with Id='{ServiceBusMessageId}', Subject='{ServiceBusMessageSubject}'.", message.MessageId, message.Subject);

        // Determine the Source Uri where specified as a property.
        var source = !string.IsNullOrEmpty(SourcePropertyName) && message.ApplicationProperties.TryGetValue(SourcePropertyName, out var src) && src is string s
            ? (Uri.TryCreate(s, UriKind.RelativeOrAbsolute, out var uri) ? uri : null)
            : null;

        // Determine if subscribed; where no single subscriber then a failure is returned from the match to be handled as configured by the caller.
        var subscribed = SubscribedManager.Match(ExecutionContext.TryGetCurrent(out var ctx, true) ? ctx : null!, args, message.Subject, source);
        if (subscribed.IsFailure)
            return subscribed.AsResult().AsTask();

        return Result.SuccessTask;
    }

    /// <inheritdoc/>
    /// <remarks>Orchestrates the execution of the underlying <see cref="SubscribedManager.ReceiveAsync(ExecutionContext, SubscribedBase, EventData, EventSubscriberArgs, CancellationToken)"/>.</remarks>
    protected override Task<Result> OnReceiveAsync(EventData @event, EventSubscriberArgs args, CancellationToken cancellationToken)
        => SubscribedManager.ReceiveAsync(ExecutionContext.TryGetCurrent(out var ctx, true) ? ctx : null!, args.Subscriber ?? throw new InvalidOperationException("The Subscriber cannot be null after a successful match; something in wrong internally."), @event, args, cancellationToken);
}