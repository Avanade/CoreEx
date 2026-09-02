namespace CoreEx.Hosting;

/// <summary>
/// Provides a reusable, generic retry <see cref="ResiliencePipeline{T}"/> factory for any <typeparamref name="TOwner"/>, retrying a bounded number of times (with backoff) for failures that satisfy a
/// caller-supplied predicate, before allowing the failure through.
/// </summary>
/// <typeparam name="TOwner">The owning <see cref="Type"/> (used only to log each retry attempt via its <see cref="ILogger"/>).</typeparam>
/// <remarks>Originally introduced within <c>CoreEx.Azure.Messaging.ServiceBus</c> for its <c>ServiceBusReceiverBase</c> (retrying an in-process message handler on <c>EventSubscriberRetryException</c>), and
/// promoted here alongside <see cref="CircuitBreakerResiliency{TOwner}"/> so other owners (e.g. a Cosmos DB change feed processor) can reuse the same bounded, classified-failure retry pattern.</remarks>
public static class RetryResiliency<TOwner>
{
    /// <summary>
    /// Creates a standardized <see cref="ResiliencePipeline{T}"/> with retry capabilities for a caller-classified subset of failures.
    /// </summary>
    /// <param name="shouldHandle">The predicate a failing <see cref="Result"/> must satisfy to be retried (e.g. a specific, known-transient exception type); a failure that does not satisfy this is never
    /// retried and is allowed straight through. Unlike <see cref="CircuitBreakerResiliency{TOwner}"/>, there is no "retry everything" default - blindly retrying an unclassified failure risks retrying one
    /// that retrying can never fix, so the caller must always specify what is worth retrying.</param>
    /// <param name="logger">Accessor for the owning <typeparamref name="TOwner"/>'s <see cref="ILogger"/>, used to log each retry attempt.</param>
    /// <param name="delay">The delay between retry attempts.</param>
    /// <param name="maxRetryAttempts">The maximum number of retry attempts.</param>
    /// <param name="backoffType">The <see cref="DelayBackoffType"/> strategy.</param>
    /// <returns>A configured <see cref="ResiliencePipeline{T}"/> instance.</returns>
    /// <remarks>The caller is responsible for flowing the owning <typeparamref name="TOwner"/> instance into the <see cref="ResilienceContext"/> via <see cref="ResilienceOwner{TOwner}.PropertyKey"/> before
    /// executing the pipeline.</remarks>
    public static ResiliencePipeline<Result> Create(Func<Result, bool> shouldHandle, Func<TOwner, ILogger> logger, TimeSpan? delay = null, int maxRetryAttempts = 3, DelayBackoffType backoffType = DelayBackoffType.Exponential)
    {
        return new ResiliencePipelineBuilder<Result>()
            .AddRetry(new RetryStrategyOptions<Result>()
            {
                ShouldHandle = args => ValueTask.FromResult(args.Outcome.Result.IsFailure && shouldHandle(args.Outcome.Result)),
                Delay = delay ?? TimeSpan.FromSeconds(2),
                MaxRetryAttempts = maxRetryAttempts,
                BackoffType = backoffType,
                OnRetry = args =>
                {
                    var ownerLogger = logger(ResilienceOwner<TOwner>.GetOwner(args.Context));
                    if (ownerLogger.IsEnabled(LogLevel.Information))
                        ownerLogger.LogInformation("Retry attempt {AttemptCount} in {AttemptDelay}ms.", args.AttemptNumber + 1, args.RetryDelay.TotalMilliseconds);

                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }
}
