namespace CoreEx.Azure.Messaging.ServiceBus;

/// <summary>
/// Provides the <see cref="ServiceBusSubscriberBase"/> invoker and underlying resiliency management.
/// </summary>
[InvokerName("CoreEx.Azure.Messaging.ServiceBus.ServiceBusReceiver")]
public class ServiceBusReceiverInvoker : InvokerBase<ServiceBusReceiverBase, IServiceBusMessageActions>
{
    /// <inheritdoc/>
    protected override async Task<TResult> OnInvokeAsync<TResult>(InvokerTracer tracer, ServiceBusReceiverBase caller, IServiceBusMessageActions args, Func<InvokerTracer, IServiceBusMessageActions, CancellationToken, Task<TResult>> func, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        // Get a resilience context from the pool to use for this execution.
        var ctx = ResilienceContextPool.Shared.Get(cancellationToken);

        try
        {
            // Set the resilience property within the context for access during processing.
            ctx.Properties.Set(ServiceBusReceiverResiliency.ResiliencePropertyKey, caller);

            // Execute the function within the resiliency policy.
            var r = await caller.Options.ReceiverResiliency.ExecuteAsync<Result, (ServiceBusReceiverInvoker Self, InvokerTracer Tracer, ServiceBusReceiverBase Caller, IServiceBusMessageActions Args, Func <InvokerTracer, IServiceBusMessageActions, CancellationToken, Task<TResult>> Func)>(async static (ctx, state) =>
            {
                // Should always return a Result and never an unhandled exception.
                var tresult = await state.Self.BaseOnInvokeAsync(state.Tracer, state.Caller, state.Args, state.Func, ctx.CancellationToken).ConfigureAwait(false);
                var result = Internal.Cast<TResult, Result>(tresult);

                if (result.IsFailure && result.Error is EventSubscriberUnhandledException)
                    await Task.Delay(state.Caller.Options.PerUnhandledErrorDelayDuration, ctx.CancellationToken).ConfigureAwait(false);

                return result;
            }, ctx, (this, tracer, caller, args, func)).ConfigureAwait(false);

            // Return the result of the execution.
            return Internal.Cast<Result, TResult>(r);
        }
        finally
        {
            // Return the resilience context to the pool.
            ResilienceContextPool.Shared.Return(ctx);

            // Emit the received duration and lag metrics.
            stopwatch.Stop();
            ServiceBus.ServiceBusMetrics.MessagesReceivedDuration.Record(stopwatch.Elapsed.TotalMilliseconds, [new(ServiceBusMetrics.SourceTagName, args.EntityPath)]);
            ServiceBus.ServiceBusMetrics.MessagesReceivedLagDuration.Record((DateTimeOffset.UtcNow - GetMessageEnqueuedTime(args)).TotalMilliseconds, [new(ServiceBusMetrics.SourceTagName, args.EntityPath)]);
        }
    }

    /// <summary>
    /// Wrapper to pass and avoid closure on the lambda.
    /// </summary>
    private Task<TResult> BaseOnInvokeAsync<TResult>(InvokerTracer tracer, ServiceBusReceiverBase caller, IServiceBusMessageActions args, Func<InvokerTracer, IServiceBusMessageActions, CancellationToken, Task<TResult>> func, CancellationToken cancellationToken)
        => base.OnInvokeAsync(tracer, caller, args, func, cancellationToken);

    /// <summary>
    /// Gets the message enqueued time from the AMQP message annotations, or returns the default value if not available.
    /// </summary>
    private static DateTimeOffset GetMessageEnqueuedTime(IServiceBusMessageActions args)
        => args.AmqpMessage.MessageAnnotations.TryGetValue("x-opt-enqueued-time", out var enqueuedTime) && enqueuedTime is DateTime dt ? dt : default;
}