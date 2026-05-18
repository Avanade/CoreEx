namespace CoreEx.Database.Postgres.Outbox;

/// <summary>
/// Provides the <see href="https://www.postgresql.org/docs/">PostgreSQL</see> <see cref="IDatabaseOutboxRelay.RelayAsync"/> execution leveraging an underlying <see cref="TimerHostedServiceBase"/>.
/// </summary>
/// <param name="serviceProvider">The <see cref="IServiceProvider"/>.</param>
/// <param name="logger">The <see cref="ILogger"/>.</param>
public sealed class PostgresOutboxRelayHostedService(IServiceProvider serviceProvider, ILogger logger)
    : DatabaseOutboxRelayHostedServiceBase<PostgresOutboxRelay>(serviceProvider, logger)
{ }