CREATE OR ALTER PROCEDURE [Products].[spOutboxBatchComplete]
  @LeaseId UNIQUEIDENTIFIER,
  @DequeuedUtc DATETIME2 NULL
AS
BEGIN
  /*
   * This is automatically generated; any changes will be lost.
   *
   * Marks a batch as completed by LeaseId, releasing the lease and making way for the next batch.
   * > Returns:
   *   0 = Success.
   *  -1 = No rows updated (e.g. already completed or invalid LeaseId).
   *  -2 = No batch to claim (e.g. all completed since claim).
   *  -3 = Unable to acquire lease (e.g. another active batch or transient error).
   */

  SET NOCOUNT ON;
  SET XACT_ABORT ON;
  SET LOCK_TIMEOUT 5000; -- Milliseconds.
  SET TRANSACTION ISOLATION LEVEL READ COMMITTED

  DECLARE @Now DATETIME2 = SYSUTCDATETIME();
  DECLARE @TenantId NVARCHAR(255);
  DECLARE @PartitionId INT;
  DECLARE @Completed TABLE (TenantId NVARCHAR(255), PartitionId INT);

  BEGIN TRY
    BEGIN TRAN;

    -- 1) Complete the batch and capture tenant/partition atomically.
    UPDATE o
      SET o.[Status] = 2,
          o.[LeaseId] = NULL,
          o.[LeaseUntilUtc] = NULL,
          o.[DequeuedUtc] = COALESCE(@DequeuedUtc, @Now)
    OUTPUT
      deleted.[TenantId],
      deleted.[PartitionId]
    INTO @Completed
    FROM [Products].[Outbox] AS o WITH (UPDLOCK, ROWLOCK)
    WHERE o.[LeaseId] = @LeaseId
      AND o.[Status] = 1;

    IF (@@ROWCOUNT = 0)
    BEGIN
      COMMIT;
      RETURN -1; -- No rows updated.
    END

    -- 2) Capture tenant/partition from first completed row.
    SELECT TOP 1
      @TenantId = TenantId,
      @PartitionId = PartitionId
    FROM @Completed;

    COMMIT;

    -- 3) Release the partition lease where identified.
    BEGIN TRY
      EXEC [Products].[spOutboxLeaseRelease] @LeaseId;
    END TRY
    BEGIN CATCH
      -- Ignore: lease will expire. Don't fail completion.
    END CATCH

    RETURN 0
  END TRY
  BEGIN CATCH
    IF (XACT_STATE() <> 0) ROLLBACK;
    THROW; -- Re-throw preserves error details to caller.
  END CATCH

END