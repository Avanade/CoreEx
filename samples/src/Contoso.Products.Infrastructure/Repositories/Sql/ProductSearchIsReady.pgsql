SELECT EXISTS (
  SELECT 1 FROM products.product_search_readiness
  WHERE profile = @Profile AND recipe = @Recipe
);
