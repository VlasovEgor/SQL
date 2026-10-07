CREATE TABLE customers
(
	customers_id SERIAL PRIMARY KEY,
	
	first_name VARCHAR NOT NULL,
	last_name VARCHAR NOT NULL,
	
	email VARCHAR NOT NULL UNIQUE
);

CREATE TABLE products
(
	products_id SERIAL PRIMARY KEY,

	quantity INT NOT NULL,
	price DECIMAL NOT NULL,

	category VARCHAR NOT NULL,
	
	CONSTRAINT CH_product_price CHECK (price >= 0),
	CONSTRAINT CH_product_quantity CHECK (quantity >= 0)
);

CREATE TABLE orders
(
	orders_id SERIAL PRIMARY KEY,
	
	customers_id INT NOT NULL,
	
	created_at TIMESTAMPTZ NOT NULL,
	
	CONSTRAINT FK_orders_customer FOREIGN KEY (customers_id) REFERENCES customers(customers_id)
);

CREATE TABLE order_item
(
	products_id INT NOT NULL,
	orders_id INT NOT NULL,
	
	quantity INT NOT NULL,
	price DECIMAL NOT NULL,

 	CONSTRAINT pk_orders_products PRIMARY KEY (orders_id, products_id),
	
	CONSTRAINT FK_order_item_order FOREIGN KEY (orders_id) REFERENCES orders(orders_id),
	CONSTRAINT FK_order_item_product FOREIGN KEY (products_id) REFERENCES products(products_id),
	
	CONSTRAINT CH_order_item_quantity CHECK (quantity > 0),
	CONSTRAINT CH_order_item_price CHECK (price >= 0)
);



alter table customers
add column balance decimal