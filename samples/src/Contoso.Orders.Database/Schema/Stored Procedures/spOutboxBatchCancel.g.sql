CREATE OR ALTER PROCEDURE [Orders].[spOutboxBatchCancel]
  @LeaseId UNIQUEIDENTIFIER,
  @BackoffSeconds INT
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

  BEGIN TRY
    BEGIN TRAN;

    UPDATE o
      SET o.[Status] = 0,
          o.[Attempts] = o.[Attempts] + 1,
          o.[AvailableUtc] = DATEADD(SECOND, @BackoffSeconds, @Now),
          o.[LeaseId] = NULL,
          o.[LeaseUntilUtc] = NULL
    FROM [Orders].[Outbox] AS o WITH (UPDLOCK, ROWLOCK)
    WHERE o.[LeaseId] = @LeaseId
      AND o.[Status] = 1;

    IF (@@ROWCOUNT = 0)
    BEGIN
      COMMIT;
      RETURN -1;
    END

    COMMIT;

    BEGIN TRY
      EXEC [Orders].[spOutboxLeaseRelease] @LeaseId;
    END TRY
    BEGIN CATCH
    END CATCH

    RETURN 0;
  END TRY
  BEGIN CATCH
    IF (XACT_STATE() <> 0) ROLLBACK;
    THROW;
  END CATCH
END