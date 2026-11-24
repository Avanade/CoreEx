WITH locked AS MATERIALIZED (
  SELECT operation_id, status, claim_owner, attempt, claim_expires_on FROM {{Table}} WHERE operation_id = @OperationId FOR UPDATE
),
observed AS MATERIALIZED (
  SELECT *, clock_timestamp() AS observed_at FROM locked
)
UPDATE {{Table}} AS t SET claim_expires_on = o.observed_at + (@DurationMs * interval '1 millisecond'), abandon_after = o.observed_at + ((@DurationMs + t.inactivity_ms) * interval '1 millisecond'), progress = COALESCE(@Progress, t.progress), updated_on = o.observed_at
FROM observed AS o
WHERE t.operation_id = o.operation_id AND o.status = 2 AND o.claim_owner COLLATE "C" = @Owner AND o.attempt = @Attempt AND o.claim_expires_on > o.observed_at
RETURNING {{Columns:t}};
