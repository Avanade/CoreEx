namespace CoreEx.Cosmos.Outbox;

/// <summary>
/// Provides the <see cref="CircuitBreakerResiliency{TOwner}"/> wiring for a <see cref="CosmosDbOutboxRelay"/>.
/// </summary>
/// <remarks>Near-verbatim mirror of <c>CoreEx.Azure.Messaging.ServiceBus</c>'s <c>ServiceBusReceiverResiliency</c> - exactly the reuse <see cref="CircuitBreakerResiliency{TOwner}"/> was promoted into base
/// <c>CoreEx</c> to enable. Unlike the Service Bus receiver's breaker (which excludes a dead-letter-classified exception type from counting), there is no exclusion predicate here - this is a happy-path-only
/// implementation with no poison-message classification yet (deferred, to be designed as one shared pattern across all CoreEx relays), so every propagated failure counts towards tripping the breaker.</remarks>
public static class CosmosDbOutboxRelayResiliency
{
    /// <summary>
    /// Creates a standardized <see cref="ResiliencePipeline{T}"/> with circuit breaker capabilities that automatically pauses and resumes a <see cref="CosmosDbOutboxRelay"/> in response to a sustained run of
    /// failures, then automatically recovers.
    /// </summary>
    /// <param name="minimumThroughput">The minimum throughput before the circuit breaker can evaluate the <paramref name="failureRatio"/>.</param>
    /// <param name="samplingDuration">The sampling duration.</param>
    /// <param name="breakDuration">The initial duration for which the circuit breaker remains open before attempting to reset (exponentially increasing with each subsequent open).</param>
    /// <param name="maxBreakDuration">The maximum duration for which the circuit breaker can remain open.</param>
    /// <param name="failureRatio">The failure ratio required to trip the circuit breaker.</param>
    /// <returns>A configured <see cref="ResiliencePipeline{T}"/> instance.</returns>
    /// <remarks>The default settings are: minimumThroughput = 5, samplingDuration = 30s, breakDuration = 15s, maxBreakDuration = 5m, failureRatio = 0.1.</remarks>
    public static ResiliencePipeline<Result> CreateRelayCircuitBreakerResiliency(int minimumThroughput = 5, TimeSpan? samplingDuration = null, TimeSpan? breakDuration = null, TimeSpan? maxBreakDuration = null, double failureRatio = 0.1)
        => CircuitBreakerResiliency<CosmosDbOutboxRelay>.Create(
            "Cosmos DB outbox relay",
            owner => owner.Logger,
            (owner, pause, cancellationToken) => owner.PauseAsync($"Cosmos DB outbox relay circuit breaker has been tripped for container '{owner.Options.ContainerId}'; will resume automatically at: {DateTimeOffset.UtcNow.Add(pause):R}.", cancellationToken),
            (owner, cancellationToken) => owner.ResumeAsync(cancellationToken),
            shouldHandle: null,
            minimumThroughput, samplingDuration, breakDuration, maxBreakDuration, failureRatio);

    /// <summary>
    /// Gets the <see cref="ResiliencePropertyKey{TOwner}"/> used to configure and manage resilience strategies for the <see cref="CosmosDbOutboxRelay"/>.
    /// </summary>
    public static ResiliencePropertyKey<CosmosDbOutboxRelay> ResiliencePropertyKey => ResilienceOwner<CosmosDbOutboxRelay>.PropertyKey;

    /// <summary>
    /// Gets the owning/invoking <see cref="CosmosDbOutboxRelay"/> from the <paramref name="context"/>.
    /// </summary>
    public static CosmosDbOutboxRelay GetOwner(ResilienceContext context) => ResilienceOwner<CosmosDbOutboxRelay>.GetOwner(context);
}
