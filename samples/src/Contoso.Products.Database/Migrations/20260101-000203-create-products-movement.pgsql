-- Migration Script.

BEGIN TRANSACTION;

CREATE TABLE "products"."movement" (
  "movement_id" VARCHAR(50) NOT NULL PRIMARY KEY,
  "reference_id" VARCHAR(50) NOT NULL,
  "movement_kind_code" VARCHAR(50) NOT NULL,
  "movement_status_code" VARCHAR(50) NOT NULL,
  "product_id" VARCHAR(50) NOT NULL,
  "quantity" NUMERIC(18, 2) DEFAULT 0 NOT NULL,
  "unit_of_measure_code" VARCHAR(50) NOT NULL,
  "created_by" VARCHAR(250) NULL,
  "created_on" TIMESTAMPTZ NULL,
  "updated_by" VARCHAR(250) NULL,
  "updated_on" TIMESTAMPTZ NULL,

  CONSTRAINT "uq_products_movement_reference_product" UNIQUE ("reference_id", "product_id")
);

COMMIT TRANSACTION;