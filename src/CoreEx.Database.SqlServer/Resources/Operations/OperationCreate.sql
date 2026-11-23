SET NOCOUNT ON;

DECLARE @created BIT = 0;
IF NOT EXISTS (SELECT 1 FROM {{Table}} WITH (UPDLOCK, HOLDLOCK, INDEX(1)) WHERE [KeyHash] = @KeyHash)
BEGIN
  DECLARE @now DATETIME2(7) = SYSUTCDATETIME();
  DECLARE @abandonAfter DATETIME2(7) = DATEADD(MILLISECOND, CAST(@InactivityMs % 86400000 AS INT), DATEADD(DAY, CAST(@InactivityMs / 86400000 AS INT), @now));

  INSERT INTO {{Table}} ([KeyHash], [OperationId], [DomainName], [TenantId], [OperationName], [InstanceId], [Status], [InitiatorId], [Attempt], [Data], [CreatedOn], [UpdatedOn], [AbandonAfter])
  VALUES (@KeyHash, @OperationId, @DomainName, @TenantId, @OperationName, @InstanceId, 1, @InitiatorId, 0, @Data, @now, @now, @abandonAfter);
  SET @created = 1;
END;

SELECT {{Columns}}, @created AS [Created] FROM {{Table}} WHERE [KeyHash] = @KeyHash;
