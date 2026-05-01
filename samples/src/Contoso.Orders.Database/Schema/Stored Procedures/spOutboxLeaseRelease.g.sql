CREATE OR ALTER PROCEDURE [Orders].[spOutboxLeaseRelease]
  @LeaseId UNIQUEIDENTIFIER
AS
BEGIN
  /*
   * This is automatically generated; any changes will be lost.
   */

  SET NOCOUNT ON;
  SET XACT_ABORT ON;
  SET LOCK_TIMEOUT 5000;
  SET TRANSACTION ISOLATION LEVEL READ COMMITTED;

  BEGIN TRY
    BEGIN TRAN;

    UPDATE ol
      SET ol.[LeaseId] = NULL,
          ol.[LeaseUntilUtc] = NULL
      FROM [Orders].[OutboxLease] AS ol WITH (UPDLOCK, ROWLOCK)
      WHERE ol.[LeaseId] = @LeaseId;

    DECLARE @Rows INT = @@ROWCOUNT;
    COMMIT;

    IF @Rows = 1 RETURN 0;
    RETURN -1;
  END TRY
  BEGIN CATCH
    IF (XACT_STATE() <> 0) ROLLBACK;
    THROW;
  END CATCH
END