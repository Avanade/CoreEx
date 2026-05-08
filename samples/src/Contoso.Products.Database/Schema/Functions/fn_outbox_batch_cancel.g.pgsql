CREATE OR REPLACE FUNCTION "products"."fn_outbox_batch_cancel"(
  p_lease_id UUID,
  p_backoff_seconds INTEGER
)
RETURNS INTEGER
LANGUAGE plpgsql
AS $$
DECLARE
  _now TIMESTAMPTZ;
  _row_count INTEGER;
BEGIN
  /*
   * This is automatically generated; any changes will be lost.
   *
   * Cancels a batch by lease_id, marking messages as pending with backoff and releasing the lease.
   * > Returns:
   *   0 = Success.
   *  -1 = No rows updated (e.g. already completed or invalid lease_id).
   */

  -- Set transaction parameters
  SET LOCAL lock_timeout = '5s';
  SET LOCAL transaction_isolation = 'read committed';

  _now := NOW() AT TIME ZONE 'UTC';

  -- 1) Cancel all rows in the batch.
  UPDATE "products"."outbox" AS o
    SET "status" = 0,
        "attempts" = o."attempts" + 1,
        "available_utc" = _now + (p_backoff_seconds || ' seconds')::INTERVAL,
        "lease_id" = NULL,
        "lease_until_utc" = NULL
    WHERE o."lease_id" = p_lease_id
      AND o."status" = 1;

  GET DIAGNOSTICS _row_count = ROW_COUNT;

  IF _row_count = 0 THEN
    RETURN -1; -- No rows updated; already completed or invalid lease_id.
  END IF;

  -- 2) Release the partition lease.
  BEGIN
    PERFORM "products"."fn_outbox_lease_release"(p_lease_id);
  EXCEPTION
    WHEN OTHERS THEN
      -- Ignore: lease will expire. Don't fail cancel.
      NULL;
  END;

  RETURN 0;

EXCEPTION
  WHEN OTHERS THEN
    RAISE; -- Re-raise preserves error details to caller.
END
$$;
