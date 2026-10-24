SELECT s.product_id AS "Id",
       s.sku = {1} AS "ExactSku",
       ts_rank_cd(s.document, websearch_to_tsquery('pg_catalog.english', {0}))::double precision AS "Rank"
FROM products.product_lexical_search s
WHERE s.recipe = {2}
  AND (s.document @@ websearch_to_tsquery('pg_catalog.english', {0}) OR s.sku = {1})
