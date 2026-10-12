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
    /// executing the pipeline.
    /// <para>This is a convenience overload of <see cref="Create{TResult}(Func{TResult, bool}, Func{TOwner, ILogger}, TimeSpan?, int, DelayBackoffType)"/> for the common case where the underlying operation
    /// has no value to carry through a retry (see <see cref="Result"/>); where a value must be carried through (e.g. the outcome of a query), use <see cref="Result{T}"/> with that overload instead.</para></remarks>
    public static ResiliencePipeline<Result> Create(Func<Result, bool> shouldHandle, Func<TOwner, ILogger> logger, TimeSpan? delay = null, int maxRetryAttempts = 3, DelayBackoffType backoffType = DelayBackoffType.Exponential)
        => Create<Result>(shouldHandle, logger, delay, maxRetryAttempts, backoffType);

    /// <summary>
    /// Creates a standardized <see cref="ResiliencePipeline{T}"/> with retry capabilities for a caller-classified subset of failures, for any <typeparamref name="TResult"/> <see cref="IResult"/> (e.g.
    /// <see cref="Result"/> or <see cref="Result{T}"/>).
    /// </summary>
    /// <typeparam name="TResult">The <see cref="IResult"/> <see cref="Type"/> (typically <see cref="Result"/> or a closed <see cref="Result{T}"/>).</typeparam>
    /// <param name="shouldHandle">The predicate a failing <typeparamref name="TResult"/> must satisfy to be retried (e.g. a specific, known-transient exception type); a failure that does not satisfy this is never
    /// retried and is allowed straight through. Unlike <see cref="CircuitBreakerResiliency{TOwner}"/>, there is no "retry everything" default - blindly retrying an unclassified failure risks retrying one
    /// that retrying can never fix, so the caller must always specify what is worth retrying.</param>
    /// <param name="logger">Accessor for the owning <typeparamref name="TOwner"/>'s <see cref="ILogger"/>, used to log each retry attempt.</param>
    /// <param name="delay">The delay between retry attempts.</param>
    /// <param name="maxRetryAttempts">The maximum number of retry attempts.</param>
    /// <param name="backoffType">The <see cref="DelayBackoffType"/> strategy.</param>
    /// <returns>A configured <see cref="ResiliencePipeline{T}"/> instance.</returns>
    /// <remarks>The caller is responsible for flowing the owning <typeparamref name="TOwner"/> instance into the <see cref="ResilienceContext"/> via <see cref="ResilienceOwner{TOwner}.PropertyKey"/> before
    /// executing the pipeline.</remarks>
    public static ResiliencePipeline<TResult> Create<TResult>(Func<TResult, bool> shouldHandle, Func<TOwner, ILogger> logger, TimeSpan? delay = null, int maxRetryAttempts = 3, DelayBackoffType backoffType = DelayBackoffType.Exponential)
        where TResult : struct, IResult
    {
        return new ResiliencePipelineBuilder<TResult>()
            .AddRetry(new RetryStrategyOptions<TResult>()
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
