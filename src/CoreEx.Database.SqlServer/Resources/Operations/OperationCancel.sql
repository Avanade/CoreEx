SET NOCOUNT ON;

-- Lock the clustered KeyHash row before observing time; the OperationId index can be locked separately.
DECLARE @keyHash BINARY(32);
SELECT @keyHash = [KeyHash] FROM {{Table}} WHERE [OperationId] = @OperationId;
SELECT @keyHash = [KeyHash] FROM {{Table}} WITH (UPDLOCK, ROWLOCK, INDEX(1)) WHERE [KeyHash] = @keyHash AND [OperationId] = @OperationId;
DECLARE @now DATETIME2(7) = SYSUTCDATETIME();

-- Accepted or retrying is cancelled immediately; running is flagged for cooperative cancellation by the claim holder.
UPDATE {{Table}} SET
  [Status] = IIF([Status] = 2, [Status], 6),
  [ErrorCode] = IIF([Status] = 2, [ErrorCode], NULL),
  [ErrorMessage] = IIF([Status] = 2, [ErrorMessage], NULL),
  [ClaimOwner] = IIF([Status] = 2, [ClaimOwner], NULL),
  [ClaimExpiresOn] = IIF([Status] = 2, [ClaimExpiresOn], NULL),
  [AbandonAfter] = IIF([Status] = 2, [AbandonAfter], NULL),
  [CompletedOn] = IIF([Status] = 2, [CompletedOn], @now),
  [PurgeAfter] = IIF([Status] = 2, [PurgeAfter], DATEADD(MILLISECOND, CAST([RetentionMs] % 86400000 AS INT), DATEADD(DAY, CAST([RetentionMs] / 86400000 AS INT), @now))),
  [CancelRequestedOn] = @now,
  [UpdatedOn] = @now
WHERE [KeyHash] = @keyHash AND [OperationId] = @OperationId AND ([Status] IN (1, 3) OR ([Status] = 2 AND [CancelRequestedOn] IS NULL));

SELECT {{Columns}} FROM {{Table}} WHERE [KeyHash] = @keyHash AND [OperationId] = @OperationId;
