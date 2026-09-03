namespace CoreEx.Database.Outbox;

/// <summary>
/// A function that executes <paramref name="work"/> with resiliency (e.g. circuit-breaker) protection applied, returning its <see cref="Result"/>.
/// </summary>
/// <param name="work">The work to execute.</param>
/// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
/// <remarks>Exists so <see cref="DatabaseOutboxRelayBase{TDatabase, TSelf}"/> (constructed fresh per relay attempt) can have each partition attempt protected by a resiliency pipeline owned by a
/// longer-lived caller (typically <see cref="DatabaseOutboxRelayHostedServiceBase"/>, a singleton) without needing to know anything about <c>CircuitBreakerResiliency{TOwner}</c>/<c>ResilienceOwner{TOwner}</c>
/// or which type owns the pipeline.</remarks>
public delegate Task<Result> DatabaseOutboxRelayResiliencyExecutor(Func<CancellationToken, Task<Result>> work, CancellationToken cancellationToken);
