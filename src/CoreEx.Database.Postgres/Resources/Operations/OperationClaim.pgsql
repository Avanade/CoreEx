WITH locked AS MATERIALIZED (
  SELECT {{Columns}} FROM {{Table}} WHERE operation_id = @OperationId FOR UPDATE
),
observed AS MATERIALIZED (
  SELECT *, clock_timestamp() AS observed_at FROM locked
),
c AS (
  UPDATE {{Table}} AS t SET status = 2, attempt = t.attempt + 1, claim_owner = @Owner, claim_expires_on = o.observed_at + (@DurationMs * interval '1 millisecond'), abandon_after = o.observed_at + ((@DurationMs + @InactivityMs) * interval '1 millisecond'), started_on = COALESCE(t.started_on, o.observed_at), updated_on = o.observed_at
  FROM observed AS o
  WHERE t.operation_id = o.operation_id AND o.status IN (1, 2, 3) AND (o.claim_owner IS NULL OR o.claim_expires_on <= o.observed_at)
  RETURNING {{Columns:t}}
)
SELECT {{Columns}}, 1 AS outcome FROM c
UNION ALL
SELECT {{Columns}}, CASE WHEN status IN (4, 5, 6, 7) THEN 3 ELSE 2 END AS outcome FROM locked WHERE NOT EXISTS (SELECT 1 FROM c);
