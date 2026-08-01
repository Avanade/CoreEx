-- Migration Script.
-- NOTE: native JSONB is a deliberate override of the bounded-text JSON-column default (see coreex-db-migration skill),
-- chosen here to demonstrate Postgres-idiomatic JSON storage with in-database query/index support.

BEGIN TRANSACTION;

ALTER TABLE "products"."product" ADD "tags_json" JSONB NULL;

COMMIT TRANSACTION;
