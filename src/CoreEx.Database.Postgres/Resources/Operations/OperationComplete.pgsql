WITH locked AS MATERIALIZED (
  SELECT operation_id, status, claim_owner, attempt, claim_expires_on FROM {{Table}} WHERE operation_id = @OperationId FOR UPDATE
),
observed AS MATERIALIZED (
  SELECT *, clock_timestamp() AS observed_at FROM locked
)
UPDATE {{Table}} AS t SET status = @Status, result_location = @ResultLocation, error_code = @ErrorCode, error_message = @ErrorMessage, claim_owner = NULL, claim_expires_on = NULL, abandon_after = NULL, completed_on = o.observed_at, updated_on = o.observed_at, purge_after = o.observed_at + (t.retention_ms * interval '1 millisecond')
FROM observed AS o
WHERE t.operation_id = o.operation_id AND o.status = 2 AND o.claim_owner COLLATE "C" = @Owner AND o.attempt = @Attempt AND o.claim_expires_on > o.observed_at
RETURNING {{Columns:t}};
