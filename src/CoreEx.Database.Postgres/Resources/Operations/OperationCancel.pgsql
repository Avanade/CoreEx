-- Accepted or retrying is cancelled immediately; running is flagged for cooperative cancellation by the claim holder.
WITH locked AS MATERIALIZED (
  SELECT {{Columns}} FROM {{Table}} WHERE operation_id = @OperationId FOR UPDATE
),
observed AS MATERIALIZED (
  SELECT *, clock_timestamp() AS observed_at FROM locked
),
u AS (
  UPDATE {{Table}} AS t SET
    status = CASE WHEN o.status = 2 THEN o.status ELSE 6 END,
    error_code = CASE WHEN o.status = 2 THEN o.error_code END,
    error_message = CASE WHEN o.status = 2 THEN o.error_message END,
    claim_owner = CASE WHEN o.status = 2 THEN o.claim_owner END,
    claim_expires_on = CASE WHEN o.status = 2 THEN o.claim_expires_on END,
    abandon_after = CASE WHEN o.status = 2 THEN o.abandon_after END,
    completed_on = CASE WHEN o.status = 2 THEN o.completed_on ELSE o.observed_at END,
    purge_after = CASE WHEN o.status = 2 THEN o.purge_after ELSE o.observed_at + (@RetentionMs * interval '1 millisecond') END,
    cancel_requested_on = o.observed_at,
    updated_on = o.observed_at
  FROM observed AS o
  WHERE t.operation_id = o.operation_id AND (o.status IN (1, 3) OR (o.status = 2 AND o.cancel_requested_on IS NULL))
  RETURNING {{Columns:t}}
)
SELECT {{Columns}} FROM u
UNION ALL
SELECT {{Columns}} FROM locked WHERE NOT EXISTS (SELECT 1 FROM u);
