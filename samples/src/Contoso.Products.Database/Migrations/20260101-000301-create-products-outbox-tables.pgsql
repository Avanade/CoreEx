-- Create table: "products"."outbox" and "products"."outbox_lease"

BEGIN;

CREATE TABLE "products"."outbox" (
  "outbox_id" BIGSERIAL NOT NULL PRIMARY KEY,
  "tenant_id" VARCHAR(255) NOT NULL,      -- Optional, null indicates no tenancy.
  "partition_id" INTEGER NOT NULL,        -- Partition number; computed in application from partition-key.
  "status" SMALLINT NOT NULL DEFAULT 0,   -- 0=Pending, 1=Processing, 2=Done.
  "enqueued_utc" TIMESTAMPTZ NOT NULL,    -- When the event was enqueued within application.
  "available_utc" TIMESTAMPTZ NOT NULL,   -- When the event is eligible for processing (retry delay).
  "dequeued_utc" TIMESTAMPTZ NULL,        -- When the event was successfully dequeued/relayed.
  "attempts" INTEGER NOT NULL DEFAULT 0,  -- Retry attempt count.

  -- Message:
  "destination" VARCHAR(255) NULL,        -- Message destination; i.e. queue/topic/etc.
  "event" TEXT NOT NULL,                  -- CloudEvent as JSON.

  -- Claim/leasing:
  "lease_id" UUID NULL,                   -- Unique identifier of the lease.
  "lease_until_utc" TIMESTAMPTZ NULL      -- Leased until UTC; after which assume released due to possible application crash.
);

CREATE INDEX "ix_products_outbox_partition_order" ON "products"."outbox" ("tenant_id", "partition_id", "outbox_id", "status", "available_utc", "lease_until_utc", "destination", "event", "attempts");
CREATE INDEX "ix_products_outbox_worker_pull" ON "products"."outbox" ("tenant_id", "partition_id", "status", "outbox_id", "available_utc");
CREATE INDEX "ix_products_outbox_clean_up" ON "products"."outbox" ("outbox_id", "dequeued_utc") WHERE "status" = 2;

CREATE TABLE "products"."outbox_lease" (
  "tenant_id" VARCHAR(255) NOT NULL,      -- Optional, null indicates no tenancy.
  "partition_id" INTEGER NOT NULL,        -- Partition number; computed in application from partition-key.
  "lease_id" UUID NULL,                   -- Unique identifier of the leasee.
  "lease_until_utc" TIMESTAMPTZ NULL,     -- Leased until UTC; after which assume released due to possible application crash.

  CONSTRAINT "pk_products_outbox_lease" PRIMARY KEY ("tenant_id", "partition_id")
);

COMMIT;