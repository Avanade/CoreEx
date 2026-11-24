SET NOCOUNT ON;

DECLARE @created BIT = 0;
IF NOT EXISTS (SELECT 1 FROM {{Table}} WITH (UPDLOCK, HOLDLOCK, INDEX(1)) WHERE [KeyHash] = @KeyHash)
BEGIN
  DECLARE @now DATETIME2(7) = SYSUTCDATETIME();
  DECLARE @abandonAfter DATETIME2(7) = DATEADD(MILLISECOND, CAST(@InactivityMs % 86400000 AS INT), DATEADD(DAY, CAST(@InactivityMs / 86400000 AS INT), @now));

  INSERT INTO {{Table}} ([KeyHash], [OperationId], [DomainName], [TenantId], [OperationName], [InstanceId], [Status], [InitiatedBy], [Attempt], [Data], [CreatedOn], [UpdatedOn], [AbandonAfter], [InactivityMs], [RetentionMs])
  VALUES (@KeyHash, @OperationId, @DomainName, @TenantId, @OperationName, @InstanceId, 1, @InitiatedBy, 0, @Data, @now, @now, @abandonAfter, @InactivityMs, @RetentionMs);
  SET @created = 1;
END;

SELECT {{Columns}}, @created AS [Created] FROM {{Table}} WHERE [KeyHash] = @KeyHash;
