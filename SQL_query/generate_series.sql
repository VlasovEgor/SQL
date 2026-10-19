SELECT customers_id, first_name, SUM(quantity * price)
FROM customers
JOIN orders USING (customers_id)
JOIN order_item USING (orders_id)
GROUP BY (customers_id)
ORDER BY SUM(quantity * price) DESC
LIMIT 10;

SELECT DATE_TRUNC('month', created_at) AS monthly_revenue,
       SUM(quantity * price)
FROM orders
JOIN order_item USING (orders_id)
WHERE created_at > CURRENT_TIMESTAMP - INTERVAL '1 year'
GROUP BY monthly_revenue
ORDER BY monthly_revenue;

SELECT *
FROM customers
LEFT JOIN orders USING (customers_id)
WHERE orders_id IS NULL;

SELECT *
FROM customers
WHERE NOT EXISTS (
    SELECT 1
    FROM orders
    WHERE orders.customers_id = customers.customers_id
);

SELECT customers_id, AVG(order_total)
FROM (
    SELECT customers_id, orders_id, SUM(price * quantity) AS order_total
    FROM orders
    JOIN order_item USING (orders_id)
    GROUP BY customers_id, orders_id
)
GROUP BY customers_id
HAVING AVG(order_total) > 5000;

SELECT customers_id, orders_id,
       ROW_NUMBER() OVER (PARTITION BY customers_id ORDER BY created_at)
FROM orders;

SELECT *
FROM (
    SELECT products_id, category, order_total,
           RANK() OVER (PARTITION BY category ORDER BY order_total DESC) AS rnk
    FROM (
        SELECT products_id, category,
               SUM(order_item.price * order_item.quantity) AS order_total
        FROM order_item
        JOIN products USING (products_id)
        GROUP BY products_id, category
    )
)
WHERE rnk <= 3;

EXPLAIN ANALYZE
SELECT *
FROM orders
WHERE customers_id = 496
  AND created_at BETWEEN '2024-01-01' AND '2026-01-01';

CREATE INDEX ix_orders_customer_created
ON orders (customers_id, created_at);

DROP INDEX ix_orders_customer_created;

CREATE INDEX idx_customers_email
ON customers (email);

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
LIMIT 3;

DO $$
DECLARE
    v_balance_1 DECIMAL;
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