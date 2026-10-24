-- Rebuildable derived state, deliberately excluded from DbEx CRUD model generation.
CREATE TABLE products.product_lexical_search (
  product_id VARCHAR(50) PRIMARY KEY REFERENCES products.product(product_id) ON DELETE CASCADE,
  recipe VARCHAR(50) NOT NULL,
  sku VARCHAR(50) NOT NULL,
  document TSVECTOR NOT NULL
);

CREATE INDEX ix_product_lexical_search_document ON products.product_lexical_search USING GIN (document);
CREATE INDEX ix_product_lexical_search_sku ON products.product_lexical_search (sku);

CREATE TABLE products.product_lexical_search_readiness (
  recipe VARCHAR(50) PRIMARY KEY,
  ready_on TIMESTAMPTZ NOT NULL
);
