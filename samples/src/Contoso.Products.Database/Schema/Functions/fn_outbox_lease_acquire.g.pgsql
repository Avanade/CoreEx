CREATE OR REPLACE FUNCTION "products"."fn_outbox_lease_acquire"(
  p_partition_id INTEGER,
  p_lease_id UUID,
  p_lease_seconds INTEGER,
  p_tenant_id VARCHAR(255) DEFAULT NULL,
  OUT p_lease_until_utc TIMESTAMPTZ,
  OUT p_return_code INTEGER
)
LANGUAGE plpgsql
AS $$
DECLARE
  _now TIMESTAMPTZ;
  _until TIMESTAMPTZ;
  _effective_tenant_id VARCHAR(255);
  _row_count INTEGER;
BEGIN
  /*
   * This is automatically generated; any changes will be lost.
   *
   * Attempts to acquire a lease for a tenant/partition, returning success status and lease until timestamp.
   * > Returns:
   *   return_code 0 = Lease acquired; caller may proceed with batch claim.
   *   return_code -1 = Lease not acquired; caller should backoff and retry.
   *
   * Notes:
   * - The function will return -1 where lease acquisition is unsuccessful, including where another active lease exists or where a transient error occurs (e.g. lock timeout).
   * - The caller should implement an appropriate retry/backoff strategy where -1 is returned, including randomization to avoid thundering herd issues.
   */

  -- Set transaction parameters
  SET LOCAL lock_timeout = '5s';
  SET LOCAL transaction_isolation = 'read committed';

  _now := NOW() AT TIME ZONE 'UTC';
  _until := _now + (p_lease_seconds || ' seconds')::INTERVAL;
  _effective_tenant_id := COALESCE(p_tenant_id, '(none)');

  -- 1) Seed the row if it does not exist, and acquire the lease if expired/empty; single atomic statement.
  INSERT INTO "products"."outbox_lease" ("tenant_id", "partition_id", "lease_id", "lease_until_utc")
    VALUES (_effective_tenant_id, p_partition_id, p_lease_id, _until)
    ON CONFLICT ("tenant_id", "partition_id") DO UPDATE
      SET "lease_id" = p_lease_id,
          "lease_until_utc" = _until
      WHERE "outbox_lease"."lease_until_utc" IS NULL OR "outbox_lease"."lease_until_utc" <= _now;

  -- 2) Return lease success status.
  GET DIAGNOSTICS _row_count = ROW_COUNT;

  IF _row_count = 1 THEN
    p_lease_until_utc := _until;
    p_return_code := 0; -- Lease successful.
  ELSE
    p_lease_until_utc := NULL;
    p_return_code := -1; -- Lease unsuccessful.
  END IF;

EXCEPTION
  WHEN OTHERS THEN
    RAISE; -- Re-raise preserves error details to caller.
END
$$;