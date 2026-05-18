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
    /// <remarks>The <paramref name="schema"/> (converted to <c>snake_case</c>) is used to qualify the database function names. The by-convention names used are as follows:
    /// <list type="bullet">
    /// <item><description><see cref="DatabaseOutboxRelayBase{TDatabase, TSelf}.ClaimBatchStatement"/> = '<c>SELECT * FROM "schema"."fn_outbox_batch_claim"(...</c>'</description></item>
    /// <item><description><see cref="DatabaseOutboxRelayBase{TDatabase, TSelf}.CompleteBatchStatement"/> = '<c>SELECT "schema"."fn_outbox_batch_complete"(...</c>'</description></item>
    /// <item><description><see cref="DatabaseOutboxRelayBase{TDatabase, TSelf}.CancelBatchStatement"/> = '<c>SELECT "schema"."fn_outbox_batch_cancel"(...</c>'</description></item>
    /// </list>
    /// The parameters are positional and must match the expected order in the database functions.</remarks>
    public override void SetStatementsByConvention(string? schema = null)
    {
        schema ??= ExecutionContext.GetService<IHostSettings>()?.DomainName;
        if (schema is not null)
        {
            schema = SentenceCase.ToSnakeCase(schema);

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
        await base.CompleteBatchAsync(args, leaseId, cancellationToken);

        if (EventPublisher.IsEmpty)
            return;

        // Capture metrics; no need to capture each as this would be diminishing returns, as the oldest and newest are the most important.
        PostgresMetrics.OutboxRelayBatchSize.Add(EventPublisher.Count);
        PostgresMetrics.OutboxRelayOldestLagDuration.Record((DateTimeOffset.UtcNow - (EventPublisher.GetEvents()[0].Event.Time ?? default)).TotalMilliseconds);
        PostgresMetrics.OutboxRelayNewestLagDuration.Record((DateTimeOffset.UtcNow - (EventPublisher.GetEvents()[^1].Event.Time ?? default)).TotalMilliseconds);
    }
}