CREATE OR ALTER PROCEDURE [Orders].[spOutboxLeaseAcquire]
  @TenantId NVARCHAR(255) = NULL,
  @PartitionId INT,
  @LeaseId UNIQUEIDENTIFIER,
  @LeaseSeconds INT,
  @LeaseUntilUtc DATETIME2 OUTPUT
AS
BEGIN
  /*
   * This is automatically generated; any changes will be lost.
   */

  SET NOCOUNT ON;
  SET XACT_ABORT ON;
  SET LOCK_TIMEOUT 5000;
  SET TRANSACTION ISOLATION LEVEL READ COMMITTED;

  DECLARE @Now DATETIME2 = SYSUTCDATETIME();
  DECLARE @Until DATETIME2 = DATEADD(SECOND, @LeaseSeconds, @Now);
  DECLARE @EffectiveTenantId NVARCHAR(255) = COALESCE(@TenantId, '(none)');

  BEGIN TRY
    BEGIN TRAN;

    IF NOT EXISTS (
      SELECT 1
      FROM [Orders].[OutboxLease] WITH (UPDLOCK, HOLDLOCK)
      WHERE [TenantId] = @EffectiveTenantId AND [PartitionId] = @PartitionId
    )
    BEGIN
      INSERT INTO [Orders].[OutboxLease] ([TenantId], [PartitionId])
      VALUES (@EffectiveTenantId, @PartitionId);
    END

    UPDATE ol
      SET ol.[LeaseId] = @LeaseId,
          ol.[LeaseUntilUtc] = @Until
      FROM [Orders].[OutboxLease] AS ol WITH (UPDLOCK, ROWLOCK)
      WHERE ol.[PartitionId] = @PartitionId
        AND ol.[TenantId] = @EffectiveTenantId
        AND (ol.[LeaseUntilUtc] IS NULL OR ol.[LeaseUntilUtc] <= @Now)
        OPTION (RECOMPILE);

    DECLARE @Rows INT = @@ROWCOUNT;
    COMMIT;

    IF @Rows = 1
    BEGIN
      SET @LeaseUntilUtc = @Until;
      RETURN 0;
    END

    SET @LeaseUntilUtc = NULL;
    RETURN -1;
  END TRY
  BEGIN CATCH
    IF (XACT_STATE() <> 0) ROLLBACK;
    THROW;
  END CATCH
END