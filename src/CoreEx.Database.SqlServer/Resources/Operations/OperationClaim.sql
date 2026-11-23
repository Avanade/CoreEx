SET NOCOUNT ON;

-- Lock the clustered KeyHash row before observing time; the OperationId index can be locked separately.
DECLARE @keyHash BINARY(32);
SELECT @keyHash = [KeyHash] FROM {{Table}} WHERE [OperationId] = @OperationId;
SELECT @keyHash = [KeyHash] FROM {{Table}} WITH (UPDLOCK, ROWLOCK, INDEX(1)) WHERE [KeyHash] = @keyHash AND [OperationId] = @OperationId;
DECLARE @now DATETIME2(7) = SYSUTCDATETIME();
DECLARE @expiresOn DATETIME2(7) = DATEADD(MILLISECOND, CAST(@DurationMs % 86400000 AS INT), DATEADD(DAY, CAST(@DurationMs / 86400000 AS INT), @now));
DECLARE @abandonAfter DATETIME2(7) = DATEADD(MILLISECOND, CAST(@InactivityMs % 86400000 AS INT), DATEADD(DAY, CAST(@InactivityMs / 86400000 AS INT), @expiresOn));
DECLARE @claimed INT;

UPDATE {{Table}} SET [Status] = 2, [Attempt] = [Attempt] + 1, [ClaimOwner] = @Owner, [ClaimExpiresOn] = @expiresOn, [AbandonAfter] = @abandonAfter, [StartedOn] = COALESCE([StartedOn], @now), [UpdatedOn] = @now
WHERE [KeyHash] = @keyHash AND [OperationId] = @OperationId AND [Status] IN (1, 2, 3) AND ([ClaimOwner] IS NULL OR [ClaimExpiresOn] <= @now);

SET @claimed = @@ROWCOUNT;

SELECT {{Columns}}, CASE WHEN @claimed = 1 THEN 1 WHEN [Status] IN (4, 5, 6, 7) THEN 3 ELSE 2 END AS [Outcome] FROM {{Table}} WHERE [KeyHash] = @keyHash AND [OperationId] = @OperationId;
