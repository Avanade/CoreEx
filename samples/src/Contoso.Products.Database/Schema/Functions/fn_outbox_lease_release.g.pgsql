CREATE OR REPLACE FUNCTION "products"."fn_outbox_lease_release"(
  p_lease_id UUID
)
RETURNS INTEGER
LANGUAGE plpgsql
AS $$
DECLARE
  _row_count INTEGER;
BEGIN
  /*
   * This is automatically generated; any changes will be lost.
   *
   * Releases a lease by lease_id, making way for the next batch.
   * > Returns:
   *   0 = Success; lease released and available for next claim.
   *  -1 = No rows updated (e.g. already released or invalid lease_id).
   *
   * Notes:
   * - The function will return -1 where release is unsuccessful, including where the lease is already released or where a transient error occurs (e.g. lock timeout).
   */

  -- Set transaction parameters
  SET LOCAL lock_timeout = '5s';
  SET LOCAL transaction_isolation = 'read committed';

  -- 1) Release lease where leasee.
  UPDATE "products"."outbox_lease" AS ol
    SET "lease_id" = NULL,
        "lease_until_utc" = NULL
    WHERE ol."lease_id" = p_lease_id;

  -- 2) Check row count and return release success status.
  GET DIAGNOSTICS _row_count = ROW_COUNT;

  IF _row_count = 1 THEN
    RETURN 0; -- Release successful.
  END IF;

  RETURN -1; -- Release unsuccessful.

EXCEPTION
  WHEN OTHERS THEN
    RAISE; -- Re-raise preserves error details to caller.
END
$$;