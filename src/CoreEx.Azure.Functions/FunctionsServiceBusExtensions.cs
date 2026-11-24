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
    /// <remarks>Unclassified and catastrophic failures are rethrown without settlement. Invocation cancellation is never classified or settled. Settlement failures propagate without a second settlement attempt. Participating
    /// <see cref="EventSubscriberBase.Behaviors"/> are notified once, after the settlement decision, via <see cref="EventSubscriberBase.NotifyDispositionAsync"/>.</remarks>
    public static async Task FunctionReceiveAsync(this ServiceBusSubscriberBase subscriber, ServiceBusReceivedMessage message, ServiceBusMessageActions actions, string entityPath,
        EventSubscriberArgs? args = null, CancellationToken cancellationToken = default)
    {
        subscriber.ThrowIfNull();
        message.ThrowIfNull();
        var recorder = new ServiceBusSettlementRecorder(new FunctionsServiceBusMessageActions(message, actions, entityPath));

        cancellationToken.ThrowIfCancellationRequested();
        args ??= new EventSubscriberArgs();

        Exception? error = null;
        var cancelled = false;
        try
        {
            Result result;
            try
            {
                result = await subscriber.ReceiveAsync(message, args, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                result = Result.Fail(ex);
            }

            if (cancellationToken.IsCancellationRequested)
            {
                cancelled = true;
                cancellationToken.ThrowIfCancellationRequested();
            }

            if (result.IsSuccess)
            {
                await recorder.CompleteMessageAsync(cancellationToken).ConfigureAwait(false);
                return;
            }

            error = result.Error;
            if (!result.Error.IsCanceledBy(cancellationToken))
            {
                var settlement = await ServiceBusReceiverBase.MessageErrorActionAsync(result.Error, recorder, cancellationToken).ConfigureAwait(false);
                if (settlement.IsSuccess)
                    return;
            }

            result.ThrowOnError();
        }
        catch (Exception ex)
        {
            // Record the failure for the disposition; the exception itself is rethrown unchanged.
            if (recorder.SettlementError is null && ex.IsCanceledBy(cancellationToken))
                cancelled = true;
            else
                error ??= recorder.SettlementError is null ? ex : null;

            throw;
        }
        finally
        {
            // Notify the participating behaviors once per delivery, after the settlement decision has been applied; failures are logged and suppressed by the subscriber.
            await subscriber.NotifyDispositionAsync(args, recorder.CreateDisposition(cancelled ? null : error, cancelled), cancellationToken).ConfigureAwait(false);
        }
    }
}
