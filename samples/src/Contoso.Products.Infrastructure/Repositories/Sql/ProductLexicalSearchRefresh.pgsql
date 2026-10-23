DELETE FROM products.product_lexical_search
WHERE CAST(@ProductId AS VARCHAR) IS NULL OR product_id = @ProductId;

INSERT INTO products.product_lexical_search (product_id, recipe, sku, document)
SELECT p.product_id, @Recipe, p.sku,
       setweight(to_tsvector('pg_catalog.english', coalesce(p.sku, '') || ' ' || coalesce(p.text, '')), 'A') ||
       setweight(to_tsvector('pg_catalog.english', coalesce(b.text, p.brand_code, '') || ' ' || coalesce(sc.text, p.sub_category_code, '') || ' ' || coalesce(c.text, sc.category_code, '')), 'B') ||
       setweight(to_tsvector('pg_catalog.english', coalesce(p.tags_json, '[]'::jsonb)), 'B') ||
       setweight(to_tsvector('pg_catalog.english', coalesce(p.description, '')), 'C')
FROM products.product p
LEFT JOIN products.brand b ON b.code = p.brand_code
LEFT JOIN products.sub_category sc ON sc.code = p.sub_category_code
LEFT JOIN products.category c ON c.code = sc.category_code
WHERE NOT p.is_deleted AND (CAST(@ProductId AS VARCHAR) IS NULL OR p.product_id = @ProductId);
