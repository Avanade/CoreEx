SET NOCOUNT ON;

-- Lock the clustered KeyHash row before observing time; the OperationId index can be locked separately.
DECLARE @keyHash BINARY(32);
SELECT @keyHash = [KeyHash] FROM {{Table}} WHERE [OperationId] = @OperationId;
SELECT @keyHash = [KeyHash] FROM {{Table}} WITH (UPDLOCK, ROWLOCK, INDEX(1)) WHERE [KeyHash] = @keyHash AND [OperationId] = @OperationId;
DECLARE @now DATETIME2(7) = SYSUTCDATETIME();
DECLARE @expiresOn DATETIME2(7) = DATEADD(MILLISECOND, CAST(@DurationMs % 86400000 AS INT), DATEADD(DAY, CAST(@DurationMs / 86400000 AS INT), @now));
DECLARE @abandonAfter DATETIME2(7) = DATEADD(MILLISECOND, CAST(@InactivityMs % 86400000 AS INT), DATEADD(DAY, CAST(@InactivityMs / 86400000 AS INT), @expiresOn));

UPDATE {{Table}} SET [ClaimExpiresOn] = @expiresOn, [AbandonAfter] = @abandonAfter, [Progress] = COALESCE(@Progress, [Progress]), [UpdatedOn] = @now
OUTPUT {{Columns:INSERTED}}
WHERE [KeyHash] = @keyHash AND [OperationId] = @OperationId AND [Status] = 2 AND CAST([ClaimOwner] AS VARBINARY(256)) = CAST(@Owner AS VARBINARY(256)) AND [Attempt] = @Attempt AND [ClaimExpiresOn] > @now;
