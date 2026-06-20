-- Migration Script.

BEGIN TRANSACTION;

CREATE TABLE "products"."product" (
  "product_id" VARCHAR(50) NOT NULL PRIMARY KEY,
  "sku" VARCHAR(50) NOT NULL UNIQUE,
  "text" VARCHAR(250) NOT NULL,
  "sub_category_code" VARCHAR(50) NOT NULL,
  "unit_of_measure_code" VARCHAR(50) NOT NULL,
  "brand_code" VARCHAR(50) NULL,
  "price" NUMERIC(18, 2) DEFAULT 0 NOT NULL,
  "is_inactive" BOOLEAN DEFAULT FALSE NOT NULL,
  "is_non_stocked" BOOLEAN DEFAULT FALSE NOT NULL,
  "created_by" VARCHAR(250) NULL,
  "created_on" TIMESTAMPTZ NULL,
  "updated_by" VARCHAR(250) NULL,
  "updated_on" TIMESTAMPTZ NULL,
  "is_deleted" BOOLEAN DEFAULT FALSE NOT NULL
);

COMMIT TRANSACTION;