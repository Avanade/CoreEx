CREATE OR ALTER PROCEDURE [dbo].[spSetSessionContext]
  @Username NVARCHAR(250) = NULL,
  @Timestamp DATETIMEOFFSET = NULL,
  @TenantId NVARCHAR(50) = NULL,
  @UserId NVARCHAR(50) = NULL
AS
BEGIN
  SET NOCOUNT ON;

  EXEC sys.sp_set_session_context @key = N'Username', @value = @Username;
  EXEC sys.sp_set_session_context @key = N'Timestamp', @value = @Timestamp;
  EXEC sys.sp_set_session_context @key = N'TenantId', @value = @TenantId;
  EXEC sys.sp_set_session_context @key = N'UserId', @value = @UserId;
END
