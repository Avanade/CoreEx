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
    /// <para>The default settings are: minimumThroughput = 5, samplingDuration = 30s, breakDuration = 15s, maxBreakDuration = 5m, failureRatio = 0.1</para>
    /// <para>Delegates the actual circuit breaker mechanics to the generic, provider-agnostic <see cref="CircuitBreakerResiliency{TOwner}"/> (shared with, e.g., a Cosmos DB change feed processor host) - this
    /// method only supplies the service-bus-specific pause/resume/log-owner/exclusion wiring.</para></remarks>
    public static ResiliencePipeline<Result> CreateReceiverCircuitBreakerResiliency(int minimumThroughput = 5, TimeSpan? samplingDuration = null, TimeSpan? breakDuration = null, TimeSpan? maxBreakDuration = null, double failureRatio = 0.1)
        => CircuitBreakerResiliency<ServiceBusReceiverBase>.Create(
            "Service bus receiver",
            owner => owner.Logger,
            (owner, pause, cancellationToken) => owner.PauseAsync($"Service bus receiver circuit breaker has been tripped; will resume automatically at: {DateTimeOffset.UtcNow.Add(pause):R}.", cancellationToken),
            (owner, cancellationToken) => owner.ResumeAsync(cancellationToken),
            result => result.Error is not EventSubscriberDeadLetterException,
            minimumThroughput, samplingDuration, breakDuration, maxBreakDuration, failureRatio);

    /// <summary>
    /// Creates a standardized <see cref="ResiliencePipeline{T}"/> with retry capabilities for transient message processing errors.
    /// </summary>
    /// <param name="delay">The delay between retry attempts.</param>
    /// <param name="maxRetryAttempts">The maximum number of retry attempts.</param>
    /// <param name="backoffType">The <see cref="DelayBackoffType"/> strategy.</param>
    /// <returns>A configured <see cref="ResiliencePipeline{T}"/> instance.</returns>
    /// <remarks>The retry strategy is configured to handle failures that are specifically of type <see cref="EventSubscriberRetryException"/>. The retry attempts will be made with a specified delay (defaults to two seconds) and
    /// backoff strategy, and the retry attempts will be logged at the information level.
    /// <para>Delegates the actual retry mechanics to the generic, provider-agnostic <see cref="RetryResiliency{TOwner}"/> (shared with, e.g., a Cosmos DB change feed processor host) - this method only
    /// supplies the service-bus-specific classification/log-owner wiring.</para></remarks>
    public static ResiliencePipeline<Result> CreateMessageRetryResiliency(TimeSpan? delay = null, int maxRetryAttempts = 3, DelayBackoffType backoffType = DelayBackoffType.Exponential)
        => RetryResiliency<ServiceBusReceiverBase>.Create(result => result.Error is EventSubscriberRetryException, owner => owner.Logger, delay, maxRetryAttempts, backoffType);

    /// <summary>
    /// Gets the <see cref="ResiliencePropertyKey{TOwner}"/> used to configure and manage resilience strategies for the <see cref="ServiceBusReceiverBase"/>.
    /// </summary>
    public static ResiliencePropertyKey<ServiceBusReceiverBase> ResiliencePropertyKey => ResilienceOwner<ServiceBusReceiverBase>.PropertyKey;

    /// <summary>
    /// Gets the owning/invoking <see cref="ServiceBusReceiverBase"/> from the <paramref name="context"/>.
    /// </summary>
    public static ServiceBusReceiverBase GetOwner(ResilienceContext context) => ResilienceOwner<ServiceBusReceiverBase>.GetOwner(context);
}