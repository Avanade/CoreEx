namespace CoreEx.Azure.Messaging.ServiceBus;

/// <summary>
/// Provides factory methods for creating standardized resilience pipelines for <see cref="ServiceBusPublisher"/> via <see cref="ServiceBusPublisher.SendResiliency"/>.
/// </summary>
public static class ServiceBusPublisherResiliency
{
    /// <summary>
    /// Creates a standardized <see cref="ResiliencePipeline{T}"/> with retry capabilities for transient send failures.
    /// </summary>
    /// <param name="delay">The delay between retry attempts.</param>
    /// <param name="maxRetryAttempts">The maximum number of retry attempts.</param>
    /// <param name="backoffType">The <see cref="DelayBackoffType"/> strategy.</param>
    /// <returns>A configured <see cref="ResiliencePipeline{T}"/> instance.</returns>
    /// <remarks>The retry strategy is configured to handle failures classified as transient by <see cref="ServiceBusErrorClassifier.IsTransient(ServiceBusException)"/>; any other failure is not retried.
    /// <para>Delegates the actual retry mechanics to the generic, provider-agnostic <see cref="RetryResiliency{TOwner}"/> (shared with, e.g., a Cosmos DB change feed processor host) - this method only
    /// supplies the service-bus-specific classification/log-owner wiring.</para></remarks>
    public static ResiliencePipeline<Result> CreateSendRetryResiliency(TimeSpan? delay = null, int maxRetryAttempts = 3, DelayBackoffType backoffType = DelayBackoffType.Exponential)
        => RetryResiliency<ServiceBusPublisher>.Create(result => result.Error is ServiceBusException sbex && ServiceBusErrorClassifier.IsTransient(sbex), owner => owner.Logger ?? NullLogger.Instance, delay, maxRetryAttempts, backoffType);
}
