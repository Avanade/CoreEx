-- Migration Script.

BEGIN TRANSACTION;

ALTER TABLE "products"."product" ADD "tags_json" JSONB NULL;

COMMIT TRANSACTION;
