namespace CoreEx.Database.Postgres.Outbox;

/// <summary>
/// Provides the <see href="https://www.postgresql.org/docs/">PostgreSQL</see> <see href="https://microservices.io/patterns/data/transactional-outbox.html">transactional outbox</see> <i>relay</i> using the destination <see cref="IEventPublisher"/>.
/// </summary>
/// <param name="database">The <see cref="PostgresDatabase"/>.</param>
/// <param name="eventPublisher">The destination <see cref="IEventPublisher"/>.</param>
/// <param name="logger">The optional <see cref="ILogger"/>.</param>
public class PostgresOutboxRelay(PostgresDatabase database, IEventPublisher eventPublisher, ILogger<PostgresOutboxRelay>? logger = null)
    : DatabaseOutboxRelayBase<PostgresDatabase, PostgresOutboxRelay>(database, eventPublisher, logger)
{
    /// <summary><inheritdoc/></summary>
    /// <param name="schema"><inheritdoc/></param>
    /// <remarks>The <paramref name="schema"/> (defaults to the <see cref="IHostSettings.DomainName"/> converted to <c>snake_case</c>) is used to qualify the database function names. The by-convention names used are as follows:
    /// <list type="bullet">
    /// <item><description><see cref="DatabaseOutboxRelayBase{TDatabase, TSelf}.ClaimBatchStatement"/> = '<c>SELECT * FROM "schema"."fn_outbox_batch_claim"(...</c>'</description></item>
    /// <item><description><see cref="DatabaseOutboxRelayBase{TDatabase, TSelf}.CompleteBatchStatement"/> = '<c>SELECT "schema"."fn_outbox_batch_complete"(...</c>'</description></item>
    /// <item><description><see cref="DatabaseOutboxRelayBase{TDatabase, TSelf}.CancelBatchStatement"/> = '<c>SELECT "schema"."fn_outbox_batch_cancel"(...</c>'</description></item>
    /// </list>
    /// The parameters are positional and must match the expected order in the database functions.</remarks>
    public override void SetStatementsByConvention(string? schema = null)
    {
        schema ??= SentenceCase.ToSnakeCase(ExecutionContext.GetService<IHostSettings>()?.DomainName);
        if (schema is not null)
        {
            ClaimBatchStatement = SqlStatement.FromText($"SELECT * FROM \"{schema}\".\"fn_outbox_batch_claim\"(@{Database.NamedColumns.PartitionIdName}, @{Database.NamedColumns.OutboxBatchSizeName}, @{Database.NamedColumns.OutboxLeaseIdName}, @{Database.NamedColumns.OutboxLeaseDurationName}, @{Database.NamedColumns.TenantIdName})");
            CompleteBatchStatement = SqlStatement.FromText($"SELECT \"{schema}\".\"fn_outbox_batch_complete\"(@{Database.NamedColumns.OutboxLeaseIdName}, @{Database.NamedColumns.OutboxDequeuedUtcName})");
            CancelBatchStatement = SqlStatement.FromText($"SELECT \"{schema}\".\"fn_outbox_batch_cancel\"(@{Database.NamedColumns.OutboxLeaseIdName}, @{Database.NamedColumns.OutboxBackoffDurationName})");
        }
    }

    /// <inheritdoc/>
    protected override bool IsTransientException(Exception exception)
    {
        if (exception is NpgsqlException nex)
        {
            if (nex.IsTransient)
                return true;

            switch (nex.SqlState)
            {
                case "40P01": return true;  // deadlock_detected: https://www.postgresql.org/docs/current/errcodes-appendix.html
                case "55P03": return true;  // lock_not_available: https://www.postgresql.org/docs/current/errcodes-appendix.html
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

        PostgresMetrics.OutboxRelayPublished.Add(EventPublisher.Count);
        RecordLagMetrics();
    }

    /// <inheritdoc/>
    protected async override Task CancelBatchAsync(DatabaseOutboxRelayArgs args, Guid leaseId, CancellationToken cancellationToken)
    {
        await base.CancelBatchAsync(args, leaseId, cancellationToken).ConfigureAwait(false);

        if (EventPublisher.IsEmpty)
            return;

        PostgresMetrics.OutboxRelayPublishFailed.Add(EventPublisher.Count);
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
        PostgresMetrics.OutboxRelayOldestLagDuration.Record((DateTimeOffset.UtcNow - (EventPublisher.GetEvents()[0].Event.Time ?? default)).TotalMilliseconds);
        PostgresMetrics.OutboxRelayNewestLagDuration.Record((DateTimeOffset.UtcNow - (EventPublisher.GetEvents()[^1].Event.Time ?? default)).TotalMilliseconds);
    }
}
