namespace CoreEx.Database.Extended;

/// <summary>
/// Represents the standard database column and/or parameters names.
/// </summary>
/// <remarks>These are used internally to map .NET properties to/from database column and/or parameter names.</remarks>
public record class DatabaseColumns
{
    /// <summary>
    /// Gets or sets the <see cref="ChangeLog.CreatedOn"/> column name.
    /// </summary>
    public string CreatedOnName { get; init; } = nameof(ChangeLog.CreatedOn);

    /// <summary>
    /// Gets or sets the <see cref="ChangeLog.CreatedBy"/> column name.
    /// </summary>
    public string CreatedByName { get; init; } = nameof(ChangeLog.CreatedBy);

    /// <summary>
    /// Gets or sets the <see cref="ChangeLog.UpdatedOn"/> column name.
    /// </summary>
    public string UpdatedOnName { get; init; } = nameof(ChangeLog.UpdatedOn);

    /// <summary>
    /// Gets or sets the <see cref="ChangeLog.UpdatedBy"/> column name.
    /// </summary>
    public string UpdatedByName { get; init; } = nameof(ChangeLog.UpdatedBy);

    /// <summary>
    /// Gets or sets the '<c>ReselectRecord</c>' parameter name.
    /// </summary>
    public string ReselectRecordName { get; init; } = "ReselectRecord";

    /// <summary>
    /// Gets or sets the <see cref="PagingArgs.Skip"/> parameter name.
    /// </summary>
    public string PagingSkipName { get; init; } = "PagingSkip";

    /// <summary>
    /// Gets or sets the <see cref="PagingArgs.Take"/> parameter name.
    /// </summary>
    public string PagingTakeName { get; init; } = "PagingTake";

    /// <summary>
    /// Gets or sets the <see cref="PagingArgs.IsCountRequested"/> parameter name.
    /// </summary>
    public string PagingCountName { get; init; } = "PagingCount";

    /// <summary>
    /// Gets or sets the '<c>RowVersion</c>' (ETag) column name.
    /// </summary>
    /// <remarks>Also see <see cref="IReadOnlyETag"/>.</remarks>
    public string RowVersionName { get; init; } = "RowVersion";

    /// <summary>
    /// Gets or sets the <see cref="IReadOnlyTenantId.TenantId"/> database column name.
    /// </summary>
    public string TenantIdName { get; init; } = "TenantId";

    /// <summary>
    /// Gets or sets the <see cref="IReadOnlyLogicallyDeleted.IsDeleted"/> database column name.
    /// </summary>
    public string IsDeletedName { get; init; } = "IsDeleted";

    /// <summary>
    /// Gets or sets the '<c>ReturnValue</c>' parameter name.
    /// </summary>
    public string ReturnValueName { get; init; } = "ReturnValue";

    /// <summary>
    /// Gets or sets the <see cref="IReferenceData"/> <see cref="IReferenceData.Id"/> database column name (defaults to <see cref="IReferenceData.Id"/>).
    /// </summary>
    public string RefDataIdName { get; init; } = nameof(IReferenceData.Id);

    /// <summary>
    /// Gets or sets the <see cref="IReferenceData.Code"/> database column name (defaults to <see cref="IReferenceData.Code"/>).
    /// </summary>
    public string RefDataCodeName { get; init; } = nameof(IReferenceData.Code);

    /// <summary>
    /// Gets or sets the <see cref="IReferenceData.Text"/> database column name (defaults to <see cref="IReferenceData.Text"/>).
    /// </summary>
    public string RefDataTextName { get; init; } = nameof(IReferenceData.Text);

    /// <summary>
    /// Gets or sets the <see cref="IReferenceData.Description"/> database column name (defaults to <see cref="IReferenceData.Description"/>).
    /// </summary>
    public string RefDataDescriptionName { get; init; } = nameof(IReferenceData.Description);

    /// <summary>
    /// Gets or sets the <see cref="IReferenceData.SortOrder"/> database column name (defaults to <see cref="IReferenceData.SortOrder"/>).
    /// </summary>
    public string RefDataSortOrderName { get; init; } = nameof(IReferenceData.SortOrder);

    /// <summary>
    /// Gets or sets the <see cref="IReferenceData.IsActive"/> database column name (defaults to <see cref="IReferenceData.IsActive"/>).
    /// </summary>
    public string RefDataIsActiveName { get; init; } = nameof(IReferenceData.IsActive);

    /// <summary>
    /// Gets or sets the <see cref="IReferenceData.StartsOn"/> database column name (defaults to <see cref="IReferenceData.StartsOn"/>).
    /// </summary>
    public string RefDataStartsOnName { get; init; } = nameof(IReferenceData.StartsOn);

    /// <summary>
    /// Gets or sets the <see cref="IReferenceData.EndsOn"/> database column name (defaults to <see cref="IReferenceData.EndsOn"/>).
    /// </summary>
    public string RefDataEndsOnName { get; init; } = nameof(IReferenceData.EndsOn);

    /// <summary>
    /// Gets or sets the partition key '<c>PartitionKey</c>' database column name.
    /// </summary>
    public string PartitionKeyName { get; init; } = "PartitionKey";

    /// <summary>
    /// Gets or sets the partition identifier/number '<c>PartitionId</c>' database column name.
    /// </summary>
    public string PartitionIdName { get; init; } = "PartitionId";

    /// <summary>
    /// Gets or sets the outbox '<c>Destination</c>' database column name.
    /// </summary>
    public string OutboxDestinationName { get; init; } = "Destination";

    /// <summary>
    /// Gets or sets the outbox '<c>Event</c>' database column name.
    /// </summary>
    public string OutboxEventName { get; init; } = "Event";

    /// <summary>
    /// Gets or sets the outbox '<c>EnqueuedUtc</c>' database column name.
    /// </summary>
    public string OutboxEnqueuedUtcName { get; init; } = "EnqueuedUtc";

    /// <summary>
    /// Gets or sets the outbox '<c>BatchSize</c>' database column name.
    /// </summary>
    public string OutboxBatchSizeName { get; init; } = "BatchSize";

    /// <summary>
    /// Gets or sets the outbox '<c>LeaseId</c>' database column name.
    /// </summary>
    public string OutboxLeaseIdName { get; init; } = "LeaseId";

    /// <summary>
    /// Gets or sets the outbox '<c>LeaseSeconds</c>' database column name.
    /// </summary>
    public string OutboxLeaseDurationName { get; init; } = "LeaseSeconds";

    /// <summary>
    /// Gets or sets the outbox '<c>DequeuedUtc</c>' database column name.
    /// </summary>
    public string OutboxDequeuedUtcName { get; init; } = "DequeuedUtc";

    /// <summary>
    /// Gets or sets the outbox '<c>BackoffSeconds</c>' database column name.
    /// </summary>
    public string OutboxBackoffDurationName { get; init; } = "BackoffSeconds";
}