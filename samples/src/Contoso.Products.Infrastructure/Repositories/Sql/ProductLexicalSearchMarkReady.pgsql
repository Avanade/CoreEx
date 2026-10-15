INSERT INTO products.product_lexical_search_readiness (recipe, ready_on)
VALUES (@Recipe, @ReadyOn)
ON CONFLICT (recipe) DO UPDATE SET ready_on = EXCLUDED.ready_on;
