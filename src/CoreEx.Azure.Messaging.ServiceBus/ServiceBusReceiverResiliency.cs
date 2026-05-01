namespace CoreEx.Azure.Messaging.ServiceBus;

/// <summary>
/// Provides factory methods for creating standardized resilience pipelines for service bus message receivers (<see cref="ServiceBusReceiverBase"/>) via either the <see cref="ServiceBusReceiverOptions"/>
/// or <see cref="ServiceBusSessionReceiverOptions"/>.
/// </summary>
/// <remarks>Their usage is intended as follows:
/// <list type="bullet">
///  <item><see cref="CreateReceiverCircuitBreakerResiliency(int, TimeSpan?, TimeSpan?, TimeSpan?, double)"/> -> <see cref="ServiceBusReceiverOptionsBase.ReceiverResiliency"/>.</item>
///  <item><see cref="CreateMessageRetryResiliency(TimeSpan?, int, DelayBackoffType)"/> -> <see cref="ServiceBusReceiverOptionsBase.MessageResiliency"/>.</item>
/// </list></remarks>
public static class ServiceBusReceiverResiliency
{
    /// <summary>
    /// Creates a standardized <see cref="ResiliencePipeline{T}"/> with circuit breaker capabilities to protect the service bus receiver from unhandled exceptions and allow for automatic recovery.
    /// </summary>
    /// <param name="minimumThroughput">The <see cref="CircuitBreakerStrategyOptions{TResult}.MinimumThroughput"/>.</param>
    /// <param name="samplingDuration">The <see cref="CircuitBreakerStrategyOptions{TResult}.SamplingDuration"/>.</param>
    /// <param name="failureRatio">The <see cref="CircuitBreakerStrategyOptions{TResult}.FailureRatio"/>.</param>
    /// <param name="breakDuration">The initial duration for which the circuit breaker remains open before attempting to reset (exponentially increasing with each subsequent open).</param>
    /// <param name="maxBreakDuration">The maximum duration for which the circuit breaker can remain open.</param>
    /// <returns>A configured <see cref="ResiliencePipeline{T}"/> instance.</returns>
    /// <remarks>The circuit breaker strategy is configured to handle failures that are not of type <see cref="EventSubscriberDeadLetterException"/>. The breaker will open based on the specified minimum throughput,
    /// sampling duration, failure ratio, and break duration settings, and will log events at the warning level.
    /// <para>The default settings are: minimumThroughput = 5, samplingDuration = 30s, breakDuration = 15s, maxBreakDuration = 5m, failureRatio = 0.1</para></remarks>
    public static ResiliencePipeline<Result> CreateReceiverCircuitBreakerResiliency(int minimumThroughput = 5, TimeSpan? samplingDuration = null, TimeSpan? breakDuration = null, TimeSpan? maxBreakDuration = null, double failureRatio = 0.1)
    {
        int circuitBreakerOpens = 0;

        samplingDuration ??= TimeSpan.FromSeconds(30);
        breakDuration ??= TimeSpan.FromSeconds(15);
        maxBreakDuration ??= TimeSpan.FromMinutes(5);

        return new ResiliencePipelineBuilder<Result>()
            .AddCircuitBreaker<Result>(new CircuitBreakerStrategyOptions<Result>()
            {
                ShouldHandle = args => ValueTask.FromResult(args.Outcome.Result.IsFailure && args.Outcome.Result.Error is not EventSubscriberDeadLetterException),
                MinimumThroughput = minimumThroughput,
                SamplingDuration = samplingDuration.Value,
                FailureRatio = failureRatio,
                BreakDurationGenerator = args =>
                {
                    // Exponential backoff on each open, similar to: 15s, 30s, 60s, ... with a cap at 5 minutes (the default).
                    var n = Interlocked.Increment(ref circuitBreakerOpens);
                    var seconds = Math.Min(breakDuration.Value.TotalSeconds * Math.Pow(2, n - 1), maxBreakDuration.Value.TotalSeconds);
                    return ValueTask.FromResult(TimeSpan.FromSeconds(seconds));
                },
                OnOpened = args =>
                {
                    // Breaker is open; pause the receiver.
                    var owner = GetOwner(args.Context);
                    if (owner.Logger.IsEnabled(LogLevel.Warning))
                        owner.Logger.LogWarning("Service bus receiver circuit breaker has been tripped for {BreakDuration}ms due to unhandled errors; receiver will be paused.", args.BreakDuration.TotalMilliseconds);

                    var pause = args.BreakDuration.Add(TimeSpan.FromMilliseconds(100)); // Add a small buffer to ensure the breaker has fully opened before resuming.

                    _ = Task.Run(async () =>
                    {
                        await owner.PauseAsync($"Service bus receiver circuit breaker has been tripped; will resume automatically at: {DateTimeOffset.UtcNow.Add(pause):R}.").ConfigureAwait(false);
                        await Task.Delay(pause).ConfigureAwait(false);
                        await owner.ResumeAsync().ConfigureAwait(false);
                    });

                    return ValueTask.CompletedTask;
                },
                OnHalfOpened = args =>
                {
                    var owner = GetOwner(args.Context);
                    if (owner.Logger.IsEnabled(LogLevel.Information))
                        owner.Logger.LogInformation("Service bus receiver circuit breaker is attempting to recover in a limited state; receiver has been resumed.");

                    return ValueTask.CompletedTask;
                },
                OnClosed = args =>
                {
                    var owner = GetOwner(args.Context);
                    if (owner.Logger.IsEnabled(LogLevel.Information))
                        owner.Logger.LogInformation("Service bus receiver circuit breaker has fully recovered; receiver is running.");

                    // Reset after recovery.
                    Interlocked.Exchange(ref circuitBreakerOpens, 0);
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    /// <summary>
    /// Creates a standardized <see cref="ResiliencePipeline{T}"/> with retry capabilities for transient message processing errors.
    /// </summary>
    /// <param name="delay">The delay between retry attempts.</param>
    /// <param name="maxRetryAttempts">The maximum number of retry attempts.</param>
    /// <param name="backoffType">The <see cref="DelayBackoffType"/> strategy.</param>
    /// <returns>A configured <see cref="ResiliencePipeline{T}"/> instance.</returns>
    /// <remarks>The retry strategy is configured to handle failures that are specifically of type <see cref="EventSubscriberRetryException"/>. The retry attempts will be made with a specified delay (defaults to two seconds) and
    /// backoff strategy, and the retry attempts will be logged at the information level.</remarks>
    public static ResiliencePipeline<Result> CreateMessageRetryResiliency(TimeSpan? delay = null, int maxRetryAttempts = 3, DelayBackoffType backoffType = DelayBackoffType.Exponential)
    {
        return new ResiliencePipelineBuilder<Result>()
            .AddRetry<Result>(new RetryStrategyOptions<Result>()
            {
                ShouldHandle = args => ValueTask.FromResult(args.Outcome.Result.IsFailure && args.Outcome.Result.Error is EventSubscriberRetryException),
                Delay = delay ?? TimeSpan.FromSeconds(2),
                MaxRetryAttempts = maxRetryAttempts,
                BackoffType = backoffType,
                OnRetry = args =>
                {
                    var owner = GetOwner(args.Context);
                    if (owner.Logger.IsEnabled(LogLevel.Information))
                        owner.Logger.LogInformation("Service bus message retry attempt {AttemptCount} in {AttemptDelay}ms.", args.AttemptNumber + 1, args.RetryDelay.TotalMilliseconds);

                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    /// <summary>
    /// Gets the <see cref="ResiliencePropertyKey"/> used to configure and manage resilience strategies for the <see cref="ServiceBusReceiverBase"/>.
    /// </summary>
    public static ResiliencePropertyKey<ServiceBusReceiverBase> ResiliencePropertyKey { get; } = new(nameof(ServiceBusReceiverBase));

    /// <summary>
    /// Gets the owning/invoking <see cref="ServiceBusReceiverBase"/> from the <paramref name="context"/>.
    /// </summary>
    public static ServiceBusReceiverBase GetOwner(ResilienceContext context) => context.Properties.GetValue(ResiliencePropertyKey, default!);
}