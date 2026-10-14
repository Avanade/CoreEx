INSERT INTO products.product_search_readiness (profile, recipe, ready_on)
VALUES (@Profile, @Recipe, @ReadyOn)
ON CONFLICT (profile, recipe) DO UPDATE SET ready_on = EXCLUDED.ready_on;
