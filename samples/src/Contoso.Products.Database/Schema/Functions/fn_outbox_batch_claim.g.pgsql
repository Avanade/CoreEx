CREATE OR REPLACE FUNCTION "products"."fn_outbox_batch_claim"(
  p_partition_id INTEGER,
  p_batch_size INTEGER,
  p_lease_id UUID,
  p_lease_seconds INTEGER,
  p_tenant_id VARCHAR(255) DEFAULT NULL
)
RETURNS TABLE (
  "outbox_id" BIGINT,
  "status" SMALLINT,
  "destination" VARCHAR(255),
  "event" TEXT,
  "attempts" INTEGER,
  "enqueued_utc" TIMESTAMPTZ,
  "available_utc" TIMESTAMPTZ,
  "lease_until_utc" TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
DECLARE
  _now TIMESTAMPTZ;
  _lease_until_utc TIMESTAMPTZ;
  _return_code INTEGER;
  _effective_tenant_id VARCHAR(255);
  _head_id BIGINT;
  _blocker_id BIGINT;
  _row_count INTEGER;
BEGIN
  /*
   * This is automatically generated; any changes will be lost.
   *
   * Claims the next batch of pending/processing messages for a tenant/partition, marking them as processing with a lease.
   * > Returns:
   *   Result set with claimed batch rows (may be empty).
   */

  -- Set transaction parameters
  SET LOCAL lock_timeout = '5s';
  SET LOCAL transaction_isolation = 'read committed';

  _now := NOW();
  _effective_tenant_id := COALESCE(p_tenant_id, '(none)');

  -- 1) Acquire a partition lease; exit where unsuccessful.
  SELECT * INTO _lease_until_utc, _return_code
  FROM "products"."fn_outbox_lease_acquire"(
    p_partition_id,
    p_lease_id,
    p_lease_seconds,
    _effective_tenant_id
  );

  IF _return_code < 0 THEN
    RETURN; -- Unable to acquire lease; return empty result set.
  END IF;

  -- 2) Claim the next batch (contiguous by outbox_id) for the tenant/partition.

  -- Determine head (first pending/processing) and first blocker (actively leased or not yet available) in a single pass.
  -- Any blocker row has status IN (0, 1) by definition, so blocker_id >= head_id is always true; no secondary scan needed.
  SELECT
    MIN(o."outbox_id") FILTER (WHERE o."status" IN (0, 1)),
    MIN(o."outbox_id") FILTER (WHERE
      (o."status" = 1 AND o."lease_until_utc" IS NOT NULL AND o."lease_until_utc" > _now)
      OR (o."status" = 0 AND o."available_utc" > _now))
  INTO _head_id, _blocker_id
  FROM "products"."outbox" o
  WHERE o."tenant_id" = _effective_tenant_id
    AND o."partition_id" = p_partition_id
    AND o."status" IN (0, 1);

  IF _head_id IS NULL THEN
    -- Release the lease.
    PERFORM "products"."fn_outbox_lease_release"(p_lease_id);
    RETURN; -- No batch to claim; return empty result set.
  END IF;

  -- Claim contiguous run from head to before blocker.
  RETURN QUERY
  WITH claim_cte AS (
    SELECT
      o."outbox_id", o."tenant_id", o."status", o."partition_id",
      o."destination", o."event", o."attempts", o."enqueued_utc",
      o."available_utc", o."lease_id", o."lease_until_utc"
    FROM "products"."outbox" o
    WHERE o."tenant_id" = _effective_tenant_id
      AND o."partition_id" = p_partition_id
      AND o."outbox_id" >= _head_id
      AND (_blocker_id IS NULL OR o."outbox_id" < _blocker_id)
      AND ((o."status" = 0 AND o."available_utc" <= _now)
        OR (o."status" = 1 AND (o."lease_until_utc" IS NULL OR o."lease_until_utc" <= _now)))
    ORDER BY o."outbox_id"
    LIMIT p_batch_size
    FOR UPDATE SKIP LOCKED
  )
  UPDATE "products"."outbox" o
    SET "status" = 1,
        "lease_id" = p_lease_id,
        "lease_until_utc" = _lease_until_utc
  FROM claim_cte
  WHERE o."outbox_id" = claim_cte."outbox_id"
  RETURNING
    o."outbox_id",
    o."status",
    o."destination",
    o."event",
    o."attempts",
    o."enqueued_utc",
    o."available_utc",
    o."lease_until_utc";

  GET DIAGNOSTICS _row_count = ROW_COUNT;

  IF _row_count = 0 THEN
    -- Release the lease.
    PERFORM "products"."fn_outbox_lease_release"(p_lease_id);
    -- No row updated; return empty result set.
  END IF;

EXCEPTION
  WHEN OTHERS THEN
    RAISE; -- Re-raise preserves error details to caller.
END
$$;