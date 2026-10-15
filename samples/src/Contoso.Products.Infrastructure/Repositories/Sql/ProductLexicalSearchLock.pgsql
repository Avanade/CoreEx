LOCK TABLE products.product, products.brand, products.sub_category, products.category IN SHARE MODE;
LOCK TABLE products.product_lexical_search IN SHARE ROW EXCLUSIVE MODE;
