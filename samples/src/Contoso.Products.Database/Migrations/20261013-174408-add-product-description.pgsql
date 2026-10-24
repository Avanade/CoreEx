-- Adds optional descriptive product text without changing existing rows.

ALTER TABLE "products"."product"
  ADD "description" VARCHAR(2000) NULL;
