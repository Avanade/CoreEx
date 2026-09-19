namespace CoreEx.Hosting;

/// <summary>
/// Provides a reusable, generic circuit breaker <see cref="ResiliencePipeline{T}"/> factory for any <typeparamref name="TOwner"/> that exposes pause/resume semantics (e.g. a message receiver or a
/// change-feed processor wrapper), protecting it from a sustained run of failures by automatically pausing it for an increasing backoff, then automatically resuming to re-test recovery.
/// </summary>
/// <typeparam name="TOwner">The owning <see cref="Type"/> to be paused/resumed when the circuit breaker trips.</typeparam>
/// <remarks>Originally introduced within <c>CoreEx.Azure.Messaging.ServiceBus</c> for its <c>ServiceBusReceiverBase</c>, and promoted here so that other push/callback-driven processors with the same
/// "the SDK owns the loop, we own start/pause/resume/stop" shape (e.g. a Cosmos DB change feed processor) can share the exact same self-pause/self-resume behaviour rather than each re-implementing it.</remarks>
public static class CircuitBreakerResiliency<TOwner>
{
    /// <summary>
    /// Creates a standardized circuit-breaker <see cref="ResiliencePipeline{T}"/> that automatically pauses and resumes the owning <typeparamref name="TOwner"/> in response to a sustained run of failures,
    /// then automatically recovers.
    /// </summary>
    /// <param name="ownerName">A short, human-readable description of <typeparamref name="TOwner"/> used only for log messages (e.g. "Service bus receiver", "Cosmos DB change feed processor").</param>
    /// <param name="logger">Accessor for the owning <typeparamref name="TOwner"/>'s <see cref="ILogger"/>.</param>
    /// <param name="pauseAsync">Invoked to pause the owner when the breaker trips; receives the computed pause duration (for logging/messaging purposes).</param>
    /// <param name="resumeAsync">Invoked to resume the owner once the pause duration has elapsed.</param>
    /// <param name="shouldHandle">An optional additional predicate a failing <see cref="Result"/> must satisfy to count towards tripping the breaker (e.g. to exclude an error type that is already handled
    /// elsewhere and should not itself pause the owner). Defaults to <see langword="null"/> (every failure counts).</param>
    /// <param name="minimumThroughput">The <see cref="CircuitBreakerStrategyOptions{TResult}.MinimumThroughput"/>.</param>
    /// <param name="samplingDuration">The <see cref="CircuitBreakerStrategyOptions{TResult}.SamplingDuration"/>.</param>
    /// <param name="breakDuration">The initial duration for which the circuit breaker remains open before attempting to reset (exponentially increasing with each subsequent open).</param>
    /// <param name="maxBreakDuration">The maximum duration for which the circuit breaker can remain open.</param>
    /// <param name="failureRatio">The <see cref="CircuitBreakerStrategyOptions{TResult}.FailureRatio"/>.</param>
    /// <returns>A configured <see cref="ResiliencePipeline{T}"/> instance.</returns>
    /// <remarks>The default settings are: minimumThroughput = 5, samplingDuration = 30s, breakDuration = 15s, maxBreakDuration = 5m, failureRatio = 0.1.
    /// <para>The caller is responsible for flowing the owning <typeparamref name="TOwner"/> instance into the <see cref="ResilienceContext"/> via <see cref="ResilienceOwner{TOwner}.PropertyKey"/> before
    /// executing the pipeline.</para></remarks>
    public static ResiliencePipeline<Result> Create(string ownerName, Func<TOwner, ILogger> logger, Func<TOwner, TimeSpan, CancellationToken, Task> pauseAsync, Func<TOwner, CancellationToken, Task> resumeAsync,
        Func<Result, bool>? shouldHandle = null, int minimumThroughput = 5, TimeSpan? samplingDuration = null, TimeSpan? breakDuration = null, TimeSpan? maxBreakDuration = null, double failureRatio = 0.1)
    {
        var circuitBreakerOpens = 0;

        samplingDuration ??= TimeSpan.FromSeconds(30);
        breakDuration ??= TimeSpan.FromSeconds(15);
        maxBreakDuration ??= TimeSpan.FromMinutes(5);

        return new ResiliencePipelineBuilder<Result>()
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions<Result>()
            {
                ShouldHandle = args => ValueTask.FromResult(args.Outcome.Result.IsFailure && (shouldHandle?.Invoke(args.Outcome.Result) ?? true)),
                MinimumThroughput = minimumThroughput,
                SamplingDuration = samplingDuration.Value,
                FailureRatio = failureRatio,
                BreakDurationGenerator = args =>
                {
                    // Exponential backoff on each open, similar to: 15s, 30s, 60s, ... with a cap at maxBreakDuration (the default: 5 minutes).
                    var n = Interlocked.Increment(ref circuitBreakerOpens);
                    var seconds = Math.Min(breakDuration.Value.TotalSeconds * Math.Pow(2, n - 1), maxBreakDuration.Value.TotalSeconds);
                    return ValueTask.FromResult(TimeSpan.FromSeconds(seconds));
                },
                OnOpened = args =>
                {
                    // Breaker is open; pause the owner.
                    var owner = ResilienceOwner<TOwner>.GetOwner(args.Context);
                    var ownerLogger = logger(owner);
                    var pause = args.BreakDuration.Add(TimeSpan.FromMilliseconds(100)); // Add a small buffer to ensure the breaker has fully opened before resuming.

                    if (ownerLogger.IsEnabled(LogLevel.Warning))
                        ownerLogger.LogWarning("{OwnerName} circuit breaker has been tripped for {BreakDuration}ms due to unhandled errors; will be paused.", ownerName, args.BreakDuration.TotalMilliseconds);

                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await pauseAsync(owner, pause, default).ConfigureAwait(false);
                            await Task.Delay(pause).ConfigureAwait(false);
                            await resumeAsync(owner, default).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            // This pause/resume is the circuit breaker's own protective mechanism; a failure here must not be silently lost as an unobserved task exception.
                            if (ownerLogger.IsEnabled(LogLevel.Error))
                                ownerLogger.LogError(ex, "{OwnerName} circuit breaker pause/resume failed; it may not have been paused/resumed as expected.", ownerName);
                        }
                    });

                    return ValueTask.CompletedTask;
                },
                OnHalfOpened = args =>
                {
                    var ownerLogger = logger(ResilienceOwner<TOwner>.GetOwner(args.Context));
                    if (ownerLogger.IsEnabled(LogLevel.Information))
                        ownerLogger.LogInformation("{OwnerName} circuit breaker is attempting to recover in a limited state; has been resumed.", ownerName);

                    return ValueTask.CompletedTask;
                },
                OnClosed = args =>
                {
                    var ownerLogger = logger(ResilienceOwner<TOwner>.GetOwner(args.Context));
                    if (ownerLogger.IsEnabled(LogLevel.Information))
                        ownerLogger.LogInformation("{OwnerName} circuit breaker has fully recovered; is running.", ownerName);

                    // Reset after recovery.
                    Interlocked.Exchange(ref circuitBreakerOpens, 0);
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }
}
