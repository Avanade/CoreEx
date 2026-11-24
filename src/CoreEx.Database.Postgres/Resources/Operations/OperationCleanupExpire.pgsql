WITH locked AS MATERIALIZED (
  SELECT key_hash, status FROM {{Table}} WHERE status IN (1, 2, 3) AND abandon_after <= statement_timestamp() ORDER BY abandon_after LIMIT @MaxCount FOR UPDATE SKIP LOCKED
),
observed AS MATERIALIZED (
  SELECT *, clock_timestamp() AS observed_at FROM locked
)
UPDATE {{Table}} AS t SET status = 7, claim_owner = NULL, claim_expires_on = NULL, abandon_after = NULL, completed_on = o.observed_at, updated_on = o.observed_at, purge_after = o.observed_at + (t.retention_ms * interval '1 millisecond')
FROM observed AS o
WHERE t.key_hash = o.key_hash
RETURNING {{Columns:t}}, o.status AS previous_status;
