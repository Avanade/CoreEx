WITH c AS (
  SELECT key_hash FROM {{Table}} WHERE status IN (4, 5, 6, 7) AND purge_after <= statement_timestamp() ORDER BY purge_after LIMIT @MaxCount FOR UPDATE SKIP LOCKED
), d AS (
  DELETE FROM {{Table}} AS t USING c WHERE t.key_hash = c.key_hash RETURNING 1
)
SELECT count(*)::int FROM d;
