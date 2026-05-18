-- Migration Script.

BEGIN TRANSACTION;

CREATE TABLE "products"."inventory" (
  "inventory_id" VARCHAR(50) NOT NULL PRIMARY KEY, -- product_id.
  "qty_on_hand" NUMERIC(18, 2) DEFAULT 0 NOT NULL
);

COMMIT TRANSACTION;