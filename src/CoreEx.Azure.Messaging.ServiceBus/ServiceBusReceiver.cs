namespace CoreEx.Azure.Messaging.ServiceBus;

/// <summary>
/// Encapsulates the <see cref="ServiceBusProcessor"/> lifetime and message receiving management.
/// </summary>
/// <typeparam name="TSubscriber">The <see cref="ServiceBusSubscriberBase"/> <see cref="Type"/> to be used for receiving each <see cref="ServiceBusReceivedMessage"/>.</typeparam>
public sealed class ServiceBusReceiver<TSubscriber> : ServiceBusReceiverBase<TSubscriber> where TSubscriber : ServiceBusSubscriberBase
{
    private readonly ServiceBusProcessor _processor;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceBusReceiver{TSubscriber}"/> class.
    /// </summary>
    /// <param name="client">The <see cref="ServiceBusClient"/>.</param>
    /// <param name="options">The <see cref="ServiceBusReceiverOptions"/>.</param>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/>.</param>
    /// <param name="logger">The <see cref="ILogger"/>.</param>
    public ServiceBusReceiver(ServiceBusClient client, ServiceBusReceiverOptions options, IServiceProvider serviceProvider, ILogger<ServiceBusReceiver<TSubscriber>> logger)
        : base(client, options, serviceProvider, logger)
    {
        var config = serviceProvider.GetRequiredService<IConfiguration>();
        var queueOrTopicName = Internal.GetValueFromConfigurationWhereApplicable(options.QueueOrTopicName, config);

        _processor = options.IsSubscription
            ? client.CreateProcessor(queueOrTopicName, Internal.GetValueFromConfigurationWhereApplicable(options.SubscriptionName!, config), options.ProcessorOptions)
            : client.CreateProcessor(queueOrTopicName, options.ProcessorOptions);

        _processor.ProcessMessageAsync += OnProcessMessageAsync;
        _processor.ProcessErrorAsync += OnProcessErrorAsync;
    }

    /// <summary>
    /// Gets the <see cref="ServiceBusReceiverOptions"/> to be used when creating the <see cref="ServiceBusReceiver"/> instance.
    /// </summary>
    public new ServiceBusReceiverOptions Options => (ServiceBusReceiverOptions)base.Options;

    /// <inheritdoc/>
    protected override Task OnStartAsync(CancellationToken cancellationToken) => _processor.StartProcessingAsync(cancellationToken);

    /// <inheritdoc/>
    protected override Task OnPauseAsync(CancellationToken cancellationToken) => _processor.StopProcessingAsync(cancellationToken);

    /// <inheritdoc/>
    protected override Task OnResumeAsync(CancellationToken cancellationToken) => _processor.StartProcessingAsync(cancellationToken);

    /// <inheritdoc/>
    protected override Task OnStopAsync(CancellationToken cancellationToken) => _processor.StopProcessingAsync(cancellationToken);

    /// <summary>
    /// Handles the processing of a message.
    /// </summary>
    private Task OnProcessMessageAsync(ProcessMessageEventArgs args) => ProcessMessageAsync(args.Message, new ProcessMessageEventArgsActions(args), args.CancellationToken);

    /// <summary>
    /// Handles the processing of an error/exception.
    /// </summary>
    private Task OnProcessErrorAsync(ProcessErrorEventArgs args)
    {
        ServiceBusErrorClassifier.ClassifyAndLogError(Logger, args);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    protected async override ValueTask DisposeAsync(bool disposing)
    {
        if (disposing)
            await _processor.DisposeAsync().ConfigureAwait(false);

        await base.DisposeAsync(disposing).ConfigureAwait(false);
    }
}