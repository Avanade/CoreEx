-- Derived search readiness is published atomically with a completed backfill.
CREATE TABLE "products"."product_search_readiness" (
  "profile" VARCHAR(250) NOT NULL,
  "recipe" VARCHAR(50) NOT NULL,
  "ready_on" TIMESTAMPTZ NOT NULL,
  PRIMARY KEY ("profile", "recipe")
);
