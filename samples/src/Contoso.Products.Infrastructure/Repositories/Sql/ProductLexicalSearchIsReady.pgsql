SELECT EXISTS (SELECT 1 FROM products.product_lexical_search_readiness WHERE recipe = @Recipe);
