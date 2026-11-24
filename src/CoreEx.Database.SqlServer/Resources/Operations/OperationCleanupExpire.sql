SET NOCOUNT ON;
DECLARE @now DATETIME2(7) = SYSUTCDATETIME();

UPDATE TOP (@MaxCount) {{Table}} WITH (READPAST) SET [Status] = 7, [ClaimOwner] = NULL, [ClaimExpiresOn] = NULL, [AbandonAfter] = NULL, [CompletedOn] = @now, [UpdatedOn] = @now, [PurgeAfter] = DATEADD(MILLISECOND, CAST([RetentionMs] % 86400000 AS INT), DATEADD(DAY, CAST([RetentionMs] / 86400000 AS INT), @now))
OUTPUT {{Columns:INSERTED}}, DELETED.[Status] AS [PreviousStatus]
WHERE [Status] IN (1, 2, 3) AND [AbandonAfter] <= @now;
