namespace CoreEx.Azure.Messaging.ServiceBus.Abstractions;

/// <summary>
/// Provides the base Azure Service Bus receiver functionality including underlying <typeparamref name="TSubscriber"/>.
/// </summary>
/// <param name="client">The <see cref="ServiceBusClient"/>.</param>
/// <param name="options">The <see cref="ServiceBusReceiverOptionsBase"/>.</param>
/// <param name="serviceProvider">The <see cref="IServiceProvider"/>.</param>
/// <param name="logger">The <see cref="ILogger"/>.</param>
public abstract class ServiceBusReceiverBase<TSubscriber>(ServiceBusClient client, ServiceBusReceiverOptionsBase options, IServiceProvider serviceProvider, ILogger<ServiceBusReceiverBase<TSubscriber>> logger)
    : ServiceBusReceiverBase(client, options, serviceProvider, logger) where TSubscriber : ServiceBusSubscriberBase
{
    private const string _destination = "destination";

    /// <inheritdoc/>
    protected sealed async override Task<Result> OnProcessMessageAsync(ServiceBusReceivedMessage message, IServiceBusMessageActions actions, CancellationToken cancellationToken)
    {
        // Get a resilience context from the pool to use for this execution.
        var ctx = ResilienceContextPool.Shared.Get(cancellationToken);

        // Wrapper for the resilience context to ensure it is returned to the pool after execution, even in the case of an exception.
        try
        {
            // Create a scope in which to perform the execution.
            await using var scope = ServiceProvider.CreateAsyncScope();

            // Instantiate and configure the execution context.
            var ec = scope.ServiceProvider.GetRequiredService<ExecutionContext>();
            Options.ExecutionContextConfigure?.Invoke(ec);

            // Get the subscriber to process the message.
            var subscriber = Options.SubscriberServiceKey is null
                ? scope.ServiceProvider.GetRequiredService<TSubscriber>()
                : scope.ServiceProvider.GetRequiredKeyedService<TSubscriber>(Options.SubscriberServiceKey);

            // Set the resilience property within the context for access during processing.
            ctx.Properties.Set(ServiceBusReceiverResiliency.ResiliencePropertyKey, this);

            // Execute the receive with resiliency.
            var esa = new EventSubscriberArgs();
            var result = await Options.MessageResiliency.ExecuteAsync<Result, (TSubscriber Subscriber, ServiceBusReceivedMessage Message, EventSubscriberArgs Args, ServiceBusReceiverBase Owner)>(async static (ctx, state) =>
            {
                // Invoke the subscriber's receive to process the message.
                Result result;
                try
                {
                    result = await state.Subscriber.ReceiveAsync(state.Message, state.Args, ctx.CancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    // Capture any exception as a Result for standardized handling.
                    result = Result.Fail(ex);
                }

                // Determine where unhandled what the final error handling will be; otherwise, success.
                return MessageUnhandledErrorDetermination(result, state.Owner.Options, state.Owner.Logger);
            }, ctx, (subscriber, message, esa, this));

            // On success complete the message and exit.
            if (result.IsSuccess)
            {
                await actions.CompleteMessageAsync(cancellationToken).ConfigureAwait(false);
                return result;
            }

            // Handle the error accordingly; a) retry error conversion, b) invoke message action, then c) pause where critical.
            return await result
                .OnFailure(r => MessageRetryErrorDetermination(r, Options, Logger))
                .OnFailureAsync(r => MessageErrorActionAsync(r.Error!, actions, cancellationToken))
                .OnFailure(async r =>
                {
                    if (r.Error is IEventSubscriberException esex && esex.ErrorHandling == ErrorHandling.Catastrophic && Options.PauseReceiverOnCatastrophicError)
                    {
                        if (Logger.IsEnabled(LogLevel.Critical))
                            Logger.LogCritical(r.Error, "A Catastrophic error has occurred within the service bus receiver for subscriber '{SubscriberTypeName}'. Abandoning the message and pausing the receiver.", typeof(TSubscriber).Name);

                        await actions.AbandonMessageAsync(r.Error, cancellationToken: cancellationToken).ConfigureAwait(false);

                        // Do not await the pause as we want to allow the message to be abandoned and the error logged without delay.
                        _ = Task.Run(() => PauseAsync("A Catastrophic error occurred within the service bus receiver.", default));
                    }
                    else
                    {
                        // Basically unhandled, and we don't really have any other course of action other than to abandon the message and log the error.
                        if (Logger.IsEnabled(LogLevel.Error))
                            Logger.LogError(r.Error, "An unhandled error has occurred within the service bus receiver for subscriber '{SubscriberTypeName}'. Abandoning the message.", typeof(TSubscriber).Name);

                        await actions.AbandonMessageAsync(r.Error, cancellationToken: cancellationToken).ConfigureAwait(false);
                    }
                }).ConfigureAwait(false);
        }
        finally
        {
            // Return the context to the pool to ensure it is available for reuse and to prevent memory leaks.
            ResilienceContextPool.Shared.Return(ctx);
        }
    }
}