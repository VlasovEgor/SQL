INSERT INTO customers(first_name, last_name, email)
SELECT 
	'first_name_' || num,
	'last_name_' || num,
	'email' || num || '@gamil.com'
FROM generate_series(1, 1000) AS num;

select * from customers;

INSERT INTO products(quantity, price, category)
SELECT 
	floor(random() * 1000) + 1 :: int,
	floor(random() * 1000),
	'category_' || floor(random() * 10)
FROM generate_series(1, 200);

select * from products;

INSERT INTO orders(customers_id, created_at)
SELECT 
	(floor(random() * 1000) + 1) :: int,
	'2020-01-01 00:00:00+03'::TIMESTAMPTZ + RANDOM() * (TIMESTAMPTZ '2026-09-23 23:59:59+03' - TIMESTAMPTZ '2020-01-01 00:00:00+03') AS random_timestamptz
FROM generate_series(1, 10000);

select * from orders;


INSERT INTO order_item(orders_id, products_id, quantity, price)
SELECT
    o.orders_id,
    p.products_id,
    (floor(random() * 10) + 1)::int,
    p.price
	
FROM orders o
CROSS JOIN products p
ORDER BY random()
LIMIT 30000;

select * from order_item;

UPDATE order_item
SET quantity = (floor(random() * 10) + 1)::int;

