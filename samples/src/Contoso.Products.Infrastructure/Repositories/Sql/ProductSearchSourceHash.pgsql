SELECT source_hash FROM products.product_search
WHERE product_id = @ProductId AND profile = @Profile AND recipe = @Recipe;
