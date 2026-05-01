namespace CoreEx.Azure.Messaging.ServiceBus.Abstractions;

/// <summary>
/// Provides the base options to be used when creating a <see cref="ServiceBusReceiverBase"/> instance.
/// </summary>
public abstract class ServiceBusReceiverOptionsBase
{
    /// <summary>
    /// Gets the default configuration key for the <see cref="QueueOrTopicName"/>.
    /// </summary>
    public const string QueueOrTopicNameAsConfigKey = "^Aspire:Azure:Messaging:ServiceBus:QueueOrTopicName";

    /// <summary>
    /// Gets the default configuration key for the <see cref="SubscriptionName"/>.
    /// </summary>
    public const string SubscriptionNameAsConfigKey = "^Aspire:Azure:Messaging:ServiceBus:SubscriptionName";

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceBusReceiverOptionsBase"/> class.
    /// </summary>
    /// <param name="queueOrTopicName">The queue or topic name to receive from.</param>
    /// <param name="subscriptionName">The subscription name to receive from (where applicable).</param>
    public ServiceBusReceiverOptionsBase(string queueOrTopicName, string? subscriptionName)
    {
        QueueOrTopicName = queueOrTopicName.ThrowIfNullOrEmpty();
        SubscriptionName = subscriptionName.ThrowIfEmpty();

        ReceiverResiliency = ServiceBusReceiverResiliency.CreateReceiverCircuitBreakerResiliency();
        MessageResiliency = ServiceBusReceiverResiliency.CreateMessageRetryResiliency();
    }

    /// <summary>
    /// Get the name of the queue or topic to receive from.
    /// </summary>
    /// <remarks>Supports the retrieval of the <see cref="QueueOrTopicName"/> from <see cref="IConfiguration"/> where prefixed with '<c>config:</c>' or '<c>^</c>', or is wrapped with '<c>%</c>'.</remarks>
    public string QueueOrTopicName { get; }

    /// <summary>
    /// Get the name of the subscription to receive from (where applicable).
    /// </summary>
    /// <remarks>Supports the retrieval of the <see cref="SubscriptionName"/> from <see cref="IConfiguration"/> where prefixed with '<c>config:</c>' or '<c>^</c>', or is wrapped with '<c>%</c>'.</remarks>
    public string? SubscriptionName { get; }

    /// <summary>
    /// Indicates whether the receiver is for a topic/subscription or a queue.
    /// </summary>
    public bool IsSubscription => SubscriptionName is not null;

    /// <summary>
    /// Gets the optional service key to be used when resolving the <see cref="ServiceBusSubscriberBase"/> from the <see cref="IServiceProvider"/>.
    /// </summary>
    public object? SubscriberServiceKey { get; set; }

    /// <summary>
    /// Gets or sets the action to configure the <see cref="ExecutionContext"/> prior to processing an individual message.
    /// </summary>
    public Action<ExecutionContext>? ExecutionContextConfigure { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="ErrorHandling"/> to use after the configured <see cref="MessageResiliency"/> has been exhausted; defaults to <see cref="ErrorHandling.Retry"/>.
    /// </summary>
    /// <remarks>Valid values are <see cref="ErrorHandling.Catastrophic"/>, <see cref="ErrorHandling.DeadLetter"/>, or <see cref="ErrorHandling.Retry"/>.</remarks>
    public ErrorHandling RetryErrorHandling
    {
        get;
        set => field = value.ThrowWhen(value => value != ErrorHandling.Catastrophic && value != ErrorHandling.DeadLetter && value != ErrorHandling.Retry);
    } = ErrorHandling.Retry;

    /// <summary>
    /// Gets or sets the unhandled <see cref="ErrorHandling"/> to use as the final catch-all handling.
    /// </summary>
    /// <remarks>Defaults to <see cref="ErrorHandling.None"/>; which indicates that unhandled exception will be rethrown allowing the receivers native exception handling to occur.
    /// <para>This is a catch all for any <see cref="Exception"/> not explicitly handled; valid values are <see cref="ErrorHandling.Catastrophic"/>, <see cref="ErrorHandling.DeadLetter"/>,
    /// <see cref="ErrorHandling.Retry"/>, or <see cref="ErrorHandling.None"/>.</para></remarks>
    public ErrorHandling UnhandledErrorHandling
    {
        get;
        set => field = value.ThrowWhen(value => value != ErrorHandling.Catastrophic && value != ErrorHandling.DeadLetter && value != ErrorHandling.Retry && value != ErrorHandling.None);
    } = ErrorHandling.None;

    /// <summary>
    /// Indicates whether to initiate a pause on the underlying receiver when a <see cref="ErrorHandling.Catastrophic"/> error occurs; defaults to <see langword="true"/>.
    /// </summary>
    /// <remarks>Where the receiver has been paused, it will then need to be manually resumed.</remarks>
    public bool PauseReceiverOnCatastrophicError { get; set; } = true;

    /// <summary>
    /// Gets or sets the <see cref="ResiliencePipeline{T}"/> used to apply the likes of circuit breaker logic to protect the service bus receiver from unhandled exceptions and allow for automatic recovery.
    /// </summary>
    public ResiliencePipeline<Result> ReceiverResiliency { get; set => field = value.ThrowIfNull(); }

    /// <summary>
    /// Gets or sets the <see cref="ResiliencePipeline{T}"/> used to apply the likes of retry logic where message processing results in a <see cref="Result.Error"/> that is an <see cref="EventSubscriberRetryException"/>.
    /// </summary>
    /// <remarks>Consider using <see cref="ServiceBusReceiverResiliency.CreateMessageRetryResiliency(TimeSpan?, int, DelayBackoffType)"/> which provides a standardized retry strategy. This is used by default.</remarks>
    public ResiliencePipeline<Result> MessageResiliency { get; set => field = value.ThrowIfNull(); }

    /// <summary>
    /// Gets or sets the duration to delay after each unhandled error (see <see cref="EventSubscriberUnhandledException"/>) occurs.
    /// </summary>
    /// <remarks>This is outside of the resiliency pipelines and is intended as a means to slow-down the processing of messages as these type of errors occur.
    /// <para>Defaults to '<c>2</c>' seconds.</para>
    /// <para>Be careful that the value is not set too high as to impact the <see cref="ReceiverResiliency"/> as this will occur within the scope of this execution.</para></remarks>
    public TimeSpan PerUnhandledErrorDelayDuration { get; set; } = TimeSpan.FromSeconds(2);
}