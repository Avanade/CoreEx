SELECT product_id AS "Id",
       embedding <=> CAST({0} AS vector(768)) AS "Distance"
FROM products.product_search
WHERE profile = {1} AND recipe = {2}
