-- Migration Script.

BEGIN TRANSACTION;

CREATE TABLE "products"."unit_of_measure" (
  "unit_of_measure_id" VARCHAR(50) NOT NULL PRIMARY KEY,
  "code" VARCHAR(50) NOT NULL UNIQUE,
  "text" VARCHAR(250) NULL,
  "is_active" BOOLEAN NULL,
  "sort_order" INTEGER NULL,
  "scale" INTEGER NOT NULL DEFAULT 0,
  "created_by" VARCHAR(250) NULL,
  "created_on" TIMESTAMPTZ NULL,
  "updated_by" VARCHAR(250) NULL,
  "updated_on" TIMESTAMPTZ NULL
);

COMMIT TRANSACTION;