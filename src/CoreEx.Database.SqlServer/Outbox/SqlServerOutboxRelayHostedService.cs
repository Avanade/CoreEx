namespace CoreEx.Database.SqlServer.Outbox;

/// <summary>
/// Provides the <see href="https://learn.microsoft.com/en-us/sql/">SQL Server</see> <see cref="IDatabaseOutboxRelay.RelayAsync"/> execution leveraging an underlying <see cref="TimerHostedServiceBase"/>.
/// </summary>
/// <param name="serviceProvider">The <see cref="IServiceProvider"/>.</param>
/// <param name="logger">The <see cref="ILogger"/>.</param>
public sealed class SqlServerOutboxRelayHostedService(IServiceProvider serviceProvider, ILogger logger)
    : DatabaseOutboxRelayHostedServiceBase<SqlServerOutboxRelay>(serviceProvider, logger)
{ }