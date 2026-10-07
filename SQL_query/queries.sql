select customers_id, first_name, sum(quantity * price)
from customers 
join orders using(customers_id)
join order_item using(orders_id)
group by (customers_id)
order by(sum(quantity * price)) desc
limit 10

select date_trunc('month', created_at) AS monthly_revenue, sum(quantity * price)
from orders
join order_item using(orders_id)
where created_at > CURRENT_TIMESTAMP - INTERVAL '1 year'
group by monthly_revenue
order by monthly_revenue

select *
from customers
left join orders using (customers_id)
where orders_id is null

select *
from customers
where not exists 
( 
	select 1
	from orders 
	where orders.customers_id = customers.customers_id
)

SELECT customers_id, AVG(order_total)
FROM
(
    SELECT customers_id, orders_id, SUM(price * quantity) AS order_total
    FROM orders
    JOIN order_item USING (orders_id)
    GROUP BY customers_id, orders_id
)
GROUP BY customers_id
HAVING AVG(order_total) > 5000;


select customers_id, orders_id, ROW_NUMBER() OVER (PARTITION BY customers_id ORDER BY created_at)
from orders

select *
from
(
	select products_id, category, order_total, rank() over (PARTITION BY category ORDER BY order_total desc) as rnk
	from	
	(
		select products_id, category, SUM(order_item.price * order_item.quantity) as order_total
		from order_item
		join products using (products_id)
		group by products_id, category
	)
)
where rnk <= 3

explain analyze
select * 
from orders 
where customers_id = 10 and created_at between '2024-01-01' and '2026-01-01'

CREATE INDEX ix_orders_customer_created
ON orders (customers_id, created_at);
SELECT *
FROM orders
where customers_id = 10 and created_at between '2024-01-01' and '2026-01-01'

DROP INDEX ix_orders_customer_created


CREATE INDEX idx_customers_email
ON customers(email);

EXPLAIN ANALYZE
SELECT *
FROM customers
WHERE email LIKE '%123%';

CREATE EXTENSION IF NOT EXISTS pg_trgm;

CREATE INDEX idx_customers_email_trgm
ON customers
USING GIN (email gin_trgm_ops);


SELECT *
FROM customers
limit 3

DO $$
DECLARE v_balance_1 decimal; 
BEGIN

	SELECT balance INTO v_balance_1
	FROM customers
	WHERE customers_id = 1
	FOR UPDATE;

    IF v_balance_1 < 1000 THEN
        RAISE EXCEPTION 'На счете недостаточно средств';
    ELSE
		UPDATE customers
    	SET balance = balance - 1000
    	WHERE customers_id = 1;
	
        UPDATE customers
        SET balance = balance + 1000
        WHERE customers_id = 2;

    END IF;
END $$;


