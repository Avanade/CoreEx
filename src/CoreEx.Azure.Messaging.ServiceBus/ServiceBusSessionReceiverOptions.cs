namespace CoreEx.Azure.Messaging.ServiceBus;

/// <summary>
/// Provides the options to be used when creating a <see cref="ServiceBusSessionReceiver"/> instance.
/// </summary>
/// <remarks>The <see cref="SessionProcessorOptions"/> defaults to a new <see cref="ServiceBusSessionProcessorOptions"/> with:
/// <list type="bullet">
///   <item><description><see cref="ServiceBusSessionProcessorOptions.ReceiveMode"/> = <see cref="ServiceBusReceiveMode.PeekLock"/>.</description></item>
///   <item><description><see cref="ServiceBusSessionProcessorOptions.AutoCompleteMessages"/> = <see langword="false"/>.</description></item>
///   <item><description><see cref="ServiceBusSessionProcessorOptions.MaxConcurrentSessions"/> = '<c>4</c>'.</description></item>
///   <item><description><see cref="ServiceBusSessionProcessorOptions.MaxAutoLockRenewalDuration"/> = '<c>00:05:00</c>' (five minutes).</description></item>
///   <item><description><see cref="ServiceBusSessionProcessorOptions.PrefetchCount"/> = '<c>0</c>'.</description></item>
/// </list>
/// </remarks>
public class ServiceBusSessionReceiverOptions : ServiceBusReceiverOptionsBase
{
    /// <summary>
    /// Create a <see cref="ServiceBusReceiverOptions"/> for the specified <paramref name="queueName"/>.
    /// </summary>
    /// <param name="queueName">The queue name.</param>
    /// <returns>The <see cref="ServiceBusReceiverOptions"/>.</returns>
    /// <remarks>Supports the retrieval of the <paramref name="queueName"/> from <see cref="IConfiguration"/> where prefixed with '<c>config:</c>' or '<c>^</c>', or is wrapped with '<c>%</c>'.</remarks>
    public static ServiceBusSessionReceiverOptions CreateForQueue(string queueName = QueueOrTopicNameAsConfigKey) => new(queueName, null);

    /// <summary>
    /// Create a <see cref="ServiceBusReceiverOptions"/> for the specified <paramref name="topicName"/> and <paramref name="subscriptionName"/>.
    /// </summary>
    /// <param name="topicName">The topic name.</param>
    /// <param name="subscriptionName">The subscription name.</param>
    /// <returns>The <see cref="ServiceBusSessionReceiverOptions"/>.</returns>
    /// <remarks>Supports the retrieval of the <paramref name="topicName"/> and <paramref name="subscriptionName"/> from <see cref="IConfiguration"/> where prefixed with '<c>config:</c>' or '<c>^</c>', or is wrapped with '<c>%</c>'.</remarks>
    public static ServiceBusSessionReceiverOptions CreateForTopicSubscription(string topicName = QueueOrTopicNameAsConfigKey, string subscriptionName = SubscriptionNameAsConfigKey)
        => new(topicName, subscriptionName.ThrowIfNullOrEmpty());

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceBusSessionReceiverOptions"/> class.
    /// </summary>
    private ServiceBusSessionReceiverOptions(string queueOrTopicName, string? subscriptionName) : base(queueOrTopicName, subscriptionName)
    {
        SessionProcessorOptions = new ServiceBusSessionProcessorOptions
        {
            ReceiveMode = ServiceBusReceiveMode.PeekLock,
            AutoCompleteMessages = false,
            MaxConcurrentSessions = 4,
            MaxAutoLockRenewalDuration = TimeSpan.FromMinutes(5),
            PrefetchCount = 0
        };
    }

    /// <summary>
    /// Gets or sets the <see cref="ServiceBusSessionProcessorOptions"/> to be used when creating a <see cref="ServiceBusSessionProcessor"/> instance.
    /// </summary>
    public ServiceBusSessionProcessorOptions SessionProcessorOptions { get; set; }
}