namespace CoreEx.Database.Postgres.Extended;

/// <summary>
/// Extends the <see cref="DatabaseColumns"/> configuring/adding PostgreSQL specific columns.
/// </summary>
/// <remarks>All are initialized using the de facto PostgreSQL snake_case convention.
/// <para>The <see cref="DatabaseColumns.RowVersionName"/> is mapped to the PostgreSQL-specific column name <c>xmin</c>. This is a PostgreSQL system column (hidden); see <see href="https://www.postgresql.org/docs/current/ddl-system-columns.html#DDL-SYSTEM-COLUMNS"/> 
/// and <see href="https://www.npgsql.org/efcore/modeling/concurrency.html"/> for more information.</para></remarks>
public record PostgresDatabaseColumns : DatabaseColumns
{
    /// <summary>
    /// Gets or sets the default <see cref="PostgresDatabaseColumns"/>.
    /// </summary>
    public static PostgresDatabaseColumns Default { get; set; } = new PostgresDatabaseColumns();

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgresDatabaseColumns"/> class.
    /// </summary>
    public PostgresDatabaseColumns()
    {
        CreatedOnName = "created_on";
        CreatedByName = "created_by";
        UpdatedOnName = "updated_on";
        UpdatedByName = "updated_by";
        ReselectRecordName = "reselect_record";
        PagingSkipName = "paging_skip";
        PagingTakeName = "paging_take";
        PagingCountName = "paging_count";
        RowVersionName = "xmin";
        TenantIdName = "tenant_id";
        IsDeletedName = "is_deleted";
        RefDataIdName = "id";
        RefDataCodeName = "code";
        RefDataTextName = "text";
        RefDataDescriptionName = "description";
        RefDataSortOrderName = "sort_order";
        RefDataIsActiveName = "is_active";
        RefDataStartsOnName = "starts_on";
        RefDataEndsOnName = "ends_on";
        PartitionKeyName = "partition_key";
        PartitionIdName = "partition_id";
        OutboxDestinationName = "destination";
        OutboxEventName = "event";
        OutboxEnqueuedUtcName = "enqueued_utc";
        OutboxBatchSizeName = "batch_size";
        OutboxLeaseIdName = "lease_id";
        OutboxLeaseDurationName = "lease_seconds";
        OutboxDequeuedUtcName = "dequeued_utc";
        OutboxBackoffDurationName = "backoff_seconds";
    }
}