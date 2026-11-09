namespace CoreEx.Azure.Functions;

/// <summary>
/// Bridges CoreEx receive results to explicit Functions Service Bus settlement.
/// </summary>
/// <remarks>Triggers must set <c>AutoCompleteMessages = false</c>. No receiver retry, circuit-breaker or pause/resume pipeline is installed.</remarks>
public static class FunctionsServiceBusExtensions
{
    /// <summary>
    /// Receives a Service Bus message using an existing CoreEx subscriber and settles its classified outcome.
    /// </summary>
    /// <param name="subscriber">The CoreEx subscriber.</param>
    /// <param name="message">The received message.</param>
    /// <param name="actions">The Functions settlement actions.</param>
    /// <param name="entityPath">The queue or topic/subscription path used for metrics, optionally containing <c>%SettingName%</c> references resolved from ambient worker configuration.</param>
    /// <param name="args">The optional subscriber arguments.</param>
    /// <param name="cancellationToken">The invocation cancellation token.</param>
    /// <returns>The receive and settlement task.</returns>
    public static Task FunctionReceiveAsync(this ServiceBusSubscriberBase subscriber, ServiceBusReceivedMessage message, ServiceBusMessageActions actions, string entityPath,
        EventSubscriberArgs? args = null, CancellationToken cancellationToken = default)
    {
        subscriber.ThrowIfNull();
        return actions.FunctionReceiveAsync(message, entityPath, ct => subscriber.ReceiveAsync(message, args, ct), new FunctionsServiceBusReceiveArgs { SubscriberOwnsCorrelation = true }, cancellationToken);
    }

    /// <summary>
    /// Processes a native message with a CoreEx result-producing handler and settles its classified outcome.
    /// </summary>
    /// <param name="actions">The Functions settlement actions.</param>
    /// <param name="message">The received message.</param>
    /// <param name="entityPath">The queue or topic/subscription path used for metrics, optionally containing <c>%SettingName%</c> references resolved from ambient worker configuration.</param>
    /// <param name="receiveAsync">The handler, responsible for its own error classification and logging.</param>
    /// <param name="args">The optional Functions receive arguments.</param>
    /// <param name="cancellationToken">The invocation cancellation token.</param>
    /// <returns>The processing and settlement task.</returns>
    /// <remarks>Unclassified and catastrophic failures are rethrown without settlement. Invocation cancellation is never classified or settled. Settlement failures propagate without a second settlement attempt.</remarks>
    public static async Task FunctionReceiveAsync(this ServiceBusMessageActions actions, ServiceBusReceivedMessage message, string entityPath, Func<CancellationToken, Task<Result>> receiveAsync,
        FunctionsServiceBusReceiveArgs? args = null, CancellationToken cancellationToken = default)
    {
        var adapter = new FunctionsServiceBusMessageActions(message, actions, entityPath);
        receiveAsync.ThrowIfNull();
        cancellationToken.ThrowIfCancellationRequested();
        message.EnrichReceiveActivity(args?.PropagateBaggage == true);

        ExecutionContext.TryGetCurrent(out var ec);
        var logger = args?.Logger ?? ec?.ServiceProvider?.GetService<Microsoft.Extensions.Logging.ILoggerFactory>()?.CreateLogger(nameof(FunctionsServiceBusExtensions));
        if (args?.SubscriberOwnsCorrelation != true && Correlation.IsEnabled() && logger is null && !Correlation.IsValid(message.CorrelationId))
            throw new InvalidOperationException("Raw Functions message processing requires ILoggerFactory or FunctionsServiceBusReceiveArgs.Logger to report invalid correlation metadata.");

        using var receiveScope = args?.SubscriberOwnsCorrelation == true ? null : Correlation.BeginReceive(message.CorrelationId, logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance);
        await ReceiveAndSettleAsync(adapter, receiveAsync, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Preserves existing receive classification and explicit settlement.</summary>
    private static async Task ReceiveAndSettleAsync(FunctionsServiceBusMessageActions adapter, Func<CancellationToken, Task<Result>> receiveAsync, CancellationToken cancellationToken)
    {
        Result result;
        try
        {
            result = await receiveAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            result = Result.Fail(ex);
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (result.IsSuccess)
        {
            await adapter.CompleteMessageAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        if (!result.Error.IsCanceledBy(cancellationToken))
        {
            var settlement = await ServiceBusReceiverBase.MessageErrorActionAsync(result.Error, adapter, cancellationToken).ConfigureAwait(false);
            if (settlement.IsSuccess)
                return;
        }

        result.ThrowOnError();
    }
}
