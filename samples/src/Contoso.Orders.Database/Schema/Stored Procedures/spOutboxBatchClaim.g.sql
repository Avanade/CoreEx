CREATE OR ALTER PROCEDURE [Orders].[spOutboxBatchClaim]
  @TenantId NVARCHAR(255) = NULL,
  @PartitionId INT,
  @BatchSize INT,
  @LeaseId UNIQUEIDENTIFIER,
  @LeaseSeconds INT
AS
BEGIN
  /*
   * This is automatically generated; any changes will be lost.
   *
   * Claims the next batch of pending/processing messages for a tenant/partition, marking them as processing with a lease.
   * > Returns:
   *   0 = Success; batch returned in result set.
   *  -1 = No rows updated (e.g. already claimed by another or transient error).
   *  -2 = No batch to claim (e.g. all completed).
   *  -3 = Unable to acquire lease (e.g. another active batch or transient error).
   */

  SET NOCOUNT ON;
  SET XACT_ABORT ON;

  DECLARE @Now DATETIME2 = SYSUTCDATETIME();
  DECLARE @LeaseUntilUtc DATETIME2;
  DECLARE @EffectiveTenantId NVARCHAR(255) = COALESCE(@TenantId, '(none)');

  SET TRANSACTION ISOLATION LEVEL READ COMMITTED
  SET LOCK_TIMEOUT 5000; -- Milliseconds.

  -- 1) Acquire a partition lease; exit where unsuccessful.
  DECLARE @RC INT;
  EXEC @RC = [Orders].[spOutboxLeaseAcquire] @EffectiveTenantId, @PartitionId, @LeaseId, @LeaseSeconds, @LeaseUntilUtc OUTPUT;
  IF (@RC < 0) RETURN -3;

  -- 2) Claim the next batch (contiguous by OutboxId) for the tenant/partition.
  BEGIN TRY
    BEGIN TRAN;

    DECLARE @HeadId BIGINT;
    DECLARE @BlockerId BIGINT;

    -- Determine head (first pending/processing) for strict contiguity.
    SELECT @HeadId = MIN(o.OutboxId)
      FROM [Orders].[Outbox] o WITH (UPDLOCK)
      WHERE o.[TenantId] = @EffectiveTenantId
        AND o.[PartitionId] = @PartitionId
        AND o.[Status] IN (0, 1)
      OPTION (RECOMPILE);

    IF @HeadId IS NULL
    BEGIN
      COMMIT;

      -- Release the lease outside transaction.
      EXEC [Orders].[spOutboxLeaseRelease] @LeaseId;
      RETURN -2; -- Nothing available.
    END

    -- Find first blocker at/after head: actively leased or not yet available.
    SELECT @BlockerId = MIN(o.OutboxId)
      FROM [Orders].[Outbox] o WITH (READPAST, UPDLOCK)
      WHERE o.[TenantId] = @EffectiveTenantId
        AND o.[PartitionId] = @PartitionId
        AND o.[OutboxId] >= @HeadId
        AND ((o.Status = 1 AND o.[LeaseUntilUtc] IS NOT NULL AND o.[LeaseUntilUtc] > @Now)
          OR (o.Status = 0 AND o.[AvailableUtc] > @Now))
      OPTION (RECOMPILE);

    -- Claim contiguous run from head to before blocker.
    ;WITH claim AS
    (
      SELECT TOP (@BatchSize)
        o.[OutboxId], o.[TenantId], o.[Status], o.[PartitionId], o.[Destination], o.[Event],
        o.[Attempts], o.[EnqueuedUtc], o.[AvailableUtc], o.[LeaseId], o.[LeaseUntilUtc]
      FROM [Orders].[Outbox] o WITH (READPAST, UPDLOCK, ROWLOCK)
      WHERE o.[TenantId] = @EffectiveTenantId
        AND o.[PartitionId] = @PartitionId
        AND o.[OutboxId] >= @HeadId
        AND (@BlockerId IS NULL OR o.[OutboxId] < @BlockerId)
        AND ((o.[Status] = 0 AND o.[AvailableUtc] <= @Now)
          OR (o.[Status] = 1 AND (o.[LeaseUntilUtc] IS NULL OR o.[LeaseUntilUtc] <= @Now)))
      ORDER BY o.OutboxId
    )
    UPDATE claim
      SET [Status] = 1,
          [LeaseId] = @LeaseId,
          [LeaseUntilUtc] = @LeaseUntilUtc
    OUTPUT
      inserted.[OutboxId],
      inserted.[TenantId],
      inserted.[Status],
      inserted.[PartitionId],
      inserted.[Destination],
      inserted.[Event],
      inserted.[Attempts],
      inserted.[EnqueuedUtc],
      inserted.[AvailableUtc],
      inserted.[LeaseUntilUtc];

      IF (@@ROWCOUNT = 0)
      BEGIN
        COMMIT;

        -- Release the lease outside transaction.
        EXEC [Orders].[spOutboxLeaseRelease] @LeaseId;
        RETURN -1; -- No rows updated.
      END

      COMMIT;
      RETURN 0;
  END TRY
  BEGIN CATCH
    IF (XACT_STATE() <> 0) ROLLBACK;
    THROW; -- Re-throw preserves error details to caller.
  END CATCH
END