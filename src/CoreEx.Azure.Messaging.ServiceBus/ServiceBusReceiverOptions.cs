namespace CoreEx.Azure.Messaging.ServiceBus;

/// <summary>
/// Provides the options to be used when creating a <see cref="ServiceBusReceiver"/> instance.
/// </summary>
/// <remarks>The <see cref="ProcessorOptions"/> defaults to a new <see cref="ServiceBusProcessorOptions"/> with:
/// <list type="bullet">
///   <item><description><see cref="ServiceBusProcessorOptions.ReceiveMode"/> = <see cref="ServiceBusReceiveMode.PeekLock"/>.</description></item>
///   <item><description><see cref="ServiceBusProcessorOptions.AutoCompleteMessages"/> = <see langword="false"/>.</description></item>
///   <item><description><see cref="ServiceBusProcessorOptions.MaxConcurrentCalls"/> = '<c>1</c>'.</description></item>
///   <item><description><see cref="ServiceBusProcessorOptions.MaxAutoLockRenewalDuration"/> = '<c>00:05:00</c>' (five minutes).</description></item>
///   <item><description><see cref="ServiceBusProcessorOptions.PrefetchCount"/> = '<c>0</c>'.</description></item>
/// </list>
/// </remarks>
public sealed class ServiceBusReceiverOptions : ServiceBusReceiverOptionsBase
{
    /// <summary>
    /// Create a <see cref="ServiceBusReceiverOptions"/> for the specified <paramref name="queueName"/>.
    /// </summary>
    /// <param name="queueName">The queue name.</param>
    /// <returns>The <see cref="ServiceBusReceiverOptions"/>.</returns>
    /// <remarks>Supports the retrieval of the <paramref name="queueName"/> from <see cref="IConfiguration"/> where prefixed with '<c>config:</c>' or '<c>^</c>', or is wrapped with '<c>%</c>'.</remarks>
    public static ServiceBusReceiverOptions CreateForQueue(string queueName = QueueOrTopicNameAsConfigKey) => new(queueName, null);

    /// <summary>
    /// Create a <see cref="ServiceBusReceiverOptions"/> for the specified <paramref name="topicName"/> and <paramref name="subscriptionName"/>.
    /// </summary>
    /// <param name="topicName">The topic name.</param>
    /// <param name="subscriptionName">The subscription name.</param>
    /// <returns>The <see cref="ServiceBusSessionReceiverOptions"/>.</returns>
    /// <remarks>Supports the retrieval of the <paramref name="topicName"/> and <paramref name="subscriptionName"/> from <see cref="IConfiguration"/> where prefixed with '<c>config:</c>' or '<c>^</c>', or is wrapped with '<c>%</c>'.</remarks>
    public static ServiceBusReceiverOptions CreateForTopicSubscription(string topicName = QueueOrTopicNameAsConfigKey, string subscriptionName = SubscriptionNameAsConfigKey)
        => new(topicName, subscriptionName.ThrowIfNullOrEmpty());

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceBusReceiverOptions"/> class.
    /// </summary>
    private ServiceBusReceiverOptions(string queueOrTopicName, string? subscriptionName) : base(queueOrTopicName, subscriptionName)
    {
        ProcessorOptions = new ServiceBusProcessorOptions
        {
            ReceiveMode = ServiceBusReceiveMode.PeekLock,
            AutoCompleteMessages = false,
            MaxConcurrentCalls = 1,
            MaxAutoLockRenewalDuration = TimeSpan.FromMinutes(5),
            PrefetchCount = 0
        };
    }

    /// <summary>
    /// Gets or sets the <see cref="ServiceBusProcessorOptions"/> to be used when creating a <see cref="ServiceBusProcessor"/> instance.
    /// </summary>
    public ServiceBusProcessorOptions ProcessorOptions { get; set; }
}