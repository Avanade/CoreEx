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
    /// <remarks>Unclassified and catastrophic failures are rethrown without settlement. Invocation cancellation is never classified or settled. Settlement failures propagate without a second settlement attempt.</remarks>
    public static async Task FunctionReceiveAsync(this ServiceBusSubscriberBase subscriber, ServiceBusReceivedMessage message, ServiceBusMessageActions actions, string entityPath,
        EventSubscriberArgs? args = null, CancellationToken cancellationToken = default)
    {
        subscriber.ThrowIfNull();
        message.ThrowIfNull();
        var adapter = new FunctionsServiceBusMessageActions(message, actions, entityPath);
        cancellationToken.ThrowIfCancellationRequested();

        Result result;
        try
        {
            result = await subscriber.ReceiveAsync(message, args, cancellationToken).ConfigureAwait(false);
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