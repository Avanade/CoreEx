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
    /// <remarks>The <paramref name="schema"/> (defaults to the <see cref="IHostSettings.DomainName"/>) is used to qualify the stored procedure names. The by-convention names used are as follows:
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
        await base.CompleteBatchAsync(args, leaseId, cancellationToken).ConfigureAwait(false);

        if (EventPublisher.IsEmpty)
            return;

        SqlServerMetrics.OutboxRelayPublished.Add(EventPublisher.Count);
        RecordLagMetrics();
    }

    /// <inheritdoc/>
    protected async override Task CancelBatchAsync(DatabaseOutboxRelayArgs args, Guid leaseId, CancellationToken cancellationToken)
    {
        await base.CancelBatchAsync(args, leaseId, cancellationToken).ConfigureAwait(false);

        if (EventPublisher.IsEmpty)
            return;

        SqlServerMetrics.OutboxRelayPublishFailed.Add(EventPublisher.Count);
        RecordLagMetrics();
    }

    /// <summary>
    /// Records the oldest/newest relay lag for the current batch, on both a successful and a failed publish attempt - so the histogram keeps reporting (and growing) for as long as a batch keeps
    /// failing, rather than going silent, which is a far more useful signal to alert on than an absent metric.
    /// </summary>
    /// <remarks>Indexes the first/last queued event rather than computing min/max - unlike Cosmos DB's Change Feed Processor (which can span multiple logical partition keys with no guaranteed
    /// overall time ordering), the claim query returns rows pre-ordered by enqueue time.</remarks>
    private void RecordLagMetrics()
    {
        SqlServerMetrics.OutboxRelayOldestLagDuration.Record((DateTimeOffset.UtcNow - (EventPublisher.GetEvents()[0].Event.Time ?? default)).TotalMilliseconds);
        SqlServerMetrics.OutboxRelayNewestLagDuration.Record((DateTimeOffset.UtcNow - (EventPublisher.GetEvents()[^1].Event.Time ?? default)).TotalMilliseconds);
    }
}
