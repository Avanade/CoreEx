namespace CoreEx.Database.SqlServer.Outbox;

/// <summary>
/// Provides the <see href="https://learn.microsoft.com/en-us/sql/">SQL Server</see> <see href="https://microservices.io/patterns/data/transactional-outbox.html">transactional outbox</see> <i>relay</i> using the destination <see cref="IEventPublisher"/>.
/// </summary>
/// <param name="database">The <see cref="SqlServerDatabase"/>.</param>
/// <param name="eventPublisher">The destination <see cref="IEventPublisher"/>.</param>
/// <param name="logger">The optional <see cref="ILogger"/>.</param>
public class SqlServerOutboxRelay(SqlServerDatabase database, IEventPublisher eventPublisher, ILogger<SqlServerOutboxRelay>? logger = null)
    : DatabaseOutboxRelayBase<SqlServerDatabase, SqlServerOutboxRelay>(database, eventPublisher, logger)
{
    /// <summary><inheritdoc/></summary>
    /// <param name="schema"><inheritdoc/></param>
    /// <remarks>The <paramref name="schema"/> is used to qualify the stored procedure names. The by-convention names used are as follows:
    /// <list type="bullet">
    /// <item><description><see cref="DatabaseOutboxRelayBase{TDatabase, TSelf}.ClaimBatchStatement"/> = '<c>[schema].[spOutboxBatchClaim]</c>'</description></item>
    /// <item><description><see cref="DatabaseOutboxRelayBase{TDatabase, TSelf}.CompleteBatchStatement"/> = '<c>[schema].[spOutboxBatchComplete]</c>'</description></item>
    /// <item><description><see cref="DatabaseOutboxRelayBase{TDatabase, TSelf}.CancelBatchStatement"/> = '<c>[schema].[spOutboxBatchCancel]</c>'</description></item>
    /// </list></remarks>
    public override void SetStatementsByConvention(string? schema = null)
    {
        schema ??= ExecutionContext.GetService<IHostSettings>()?.DomainName;
        if (schema is not null)
        {
            ClaimBatchStatement = SqlStatement.StoredProcedure($"[{schema}].[spOutboxBatchClaim]");
            CompleteBatchStatement = SqlStatement.StoredProcedure($"[{schema}].[spOutboxBatchComplete]");
            CancelBatchStatement = SqlStatement.StoredProcedure($"[{schema}].[spOutboxBatchCancel]");
        }
    }

    /// <inheritdoc/>
    protected override bool IsTransientException(Exception exception)
    {
        if (exception is SqlException sex && sex.Errors.Count > 0)
        {
            switch (sex.Errors[0].Number)
            {
                case 1205: return true;  // Deadlock: https://learn.microsoft.com/en-us/sql/relational-databases/errors-events/mssqlserver-1205-database-engine-error
            }
        }

        return base.IsTransientException(exception);
    }

    /// <inheritdoc/>
    protected async override Task CompleteBatchAsync(DatabaseOutboxRelayArgs args, Guid leaseId, CancellationToken cancellationToken)
    {
        await base.CompleteBatchAsync(args, leaseId, cancellationToken);

        if (EventPublisher.IsEmpty)
            return;

        // Capture metrics; no need to capture each as this would be diminishing returns, as the oldest and newest are the most important.
        SqlServerMetrics.OutboxRelayBatchSize.Add(EventPublisher.Count);
        SqlServerMetrics.OutboxRelayOldestLagDuration.Record((DateTimeOffset.UtcNow - (EventPublisher.GetEvents()[0].Event.Time ?? default)).TotalMilliseconds);
        SqlServerMetrics.OutboxRelayNewestLagDuration.Record((DateTimeOffset.UtcNow - (EventPublisher.GetEvents()[^1].Event.Time ?? default)).TotalMilliseconds);
    }
}