INSERT INTO products.product_search (product_id, profile, recipe, source_hash, document, embedding, indexed_on)
VALUES (@ProductId, @Profile, @Recipe, @SourceHash, @Document, CAST(@Embedding AS vector(768)), @IndexedOn)
ON CONFLICT (product_id, profile, recipe) DO UPDATE SET
  source_hash = EXCLUDED.source_hash,
  document = EXCLUDED.document,
  embedding = EXCLUDED.embedding,
  indexed_on = EXCLUDED.indexed_on;
