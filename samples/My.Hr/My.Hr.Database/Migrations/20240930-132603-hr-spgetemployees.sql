CREATE OR ALTER PROCEDURE [Hr].[spGetEmployees]
  @Ids AS NVARCHAR(MAX)
AS
BEGIN
  SET NOCOUNT ON;

  -- Select the requested data.
  SELECT * FROM [Hr].[Employee] WHERE [EmployeeId] IN (SELECT VALUE FROM OPENJSON(@Ids))
END