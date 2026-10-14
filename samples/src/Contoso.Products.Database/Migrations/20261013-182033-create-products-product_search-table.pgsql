-- Derived search data is maintained explicitly, not as a CRUD entity in DbEx.
CREATE EXTENSION IF NOT EXISTS vector;

CREATE TABLE "products"."product_search" (
  "product_id" VARCHAR(50) NOT NULL REFERENCES "products"."product" ("product_id") ON DELETE CASCADE,
  "profile" VARCHAR(250) NOT NULL,
  "recipe" VARCHAR(50) NOT NULL,
  "source_hash" CHAR(64) NOT NULL,
  "document" TEXT NOT NULL,
  "embedding" vector(768) NOT NULL CHECK (vector_norm("embedding") > 0),
  "indexed_on" TIMESTAMPTZ NOT NULL,
  PRIMARY KEY ("product_id", "profile", "recipe")
);
