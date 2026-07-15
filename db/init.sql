-- Orders table for data analysis demonstration
CREATE TABLE IF NOT EXISTS orders (
    order_id        INTEGER PRIMARY KEY AUTOINCREMENT,
    customer_id     TEXT    NOT NULL,
    order_date      TEXT    NOT NULL,   -- ISO 8601: YYYY-MM-DD
    gross_revenue   REAL    NOT NULL,
    net_revenue     REAL    NOT NULL,
    sales_channel   TEXT    NOT NULL,   -- 'online', 'in-store', 'wholesale', 'marketplace'
    product_category TEXT   NOT NULL,
    quantity        INTEGER NOT NULL DEFAULT 1,
    unit_price      REAL    NOT NULL,
    discount_amount REAL    NOT NULL DEFAULT 0.0,
    tax_amount      REAL    NOT NULL DEFAULT 0.0,
    order_status    TEXT    NOT NULL DEFAULT 'completed', -- 'completed', 'pending', 'refunded', 'cancelled'
    region          TEXT    NOT NULL,
    payment_method  TEXT    NOT NULL,   -- 'credit_card', 'paypal', 'bank_transfer', 'cash'
    currency        TEXT    NOT NULL DEFAULT 'USD'
);

-- Indexes useful for analytical queries
CREATE INDEX IF NOT EXISTS idx_orders_customer   ON orders (customer_id);
CREATE INDEX IF NOT EXISTS idx_orders_date       ON orders (order_date);
CREATE INDEX IF NOT EXISTS idx_orders_channel    ON orders (sales_channel);
CREATE INDEX IF NOT EXISTS idx_orders_status     ON orders (order_status);
CREATE INDEX IF NOT EXISTS idx_orders_region     ON orders (region);

-- Seed data (demonstration)
INSERT INTO orders (customer_id, order_date, gross_revenue, net_revenue, sales_channel, product_category, quantity, unit_price, discount_amount, tax_amount, order_status, region, payment_method, currency) VALUES
('CUST-001', '2025-01-05', 120.00,  108.00, 'online',      'Electronics',  1, 120.00,  0.00,  12.00, 'completed', 'North America', 'credit_card',   'USD'),
('CUST-002', '2025-01-08', 250.00,  225.00, 'in-store',    'Apparel',      3,  90.00,  15.00, 25.00, 'completed', 'Europe',        'cash',          'EUR'),
('CUST-003', '2025-01-12',  45.50,   40.95, 'marketplace', 'Books',        2,  25.00,   4.50,  5.00, 'completed', 'Asia Pacific',  'paypal',        'USD'),
('CUST-001', '2025-01-20', 310.00,  279.00, 'online',      'Electronics',  2, 160.00,  10.00, 31.00, 'completed', 'North America', 'credit_card',   'USD'),
('CUST-004', '2025-02-02',  80.00,   72.00, 'wholesale',   'Food & Bev',   8,  10.00,   0.00,  8.00, 'completed', 'Europe',        'bank_transfer', 'EUR'),
('CUST-005', '2025-02-14', 199.99,  179.99, 'online',      'Home & Garden',1, 199.99,   0.00, 20.00, 'completed', 'North America', 'credit_card',   'USD'),
('CUST-002', '2025-02-19',  55.00,   49.50, 'in-store',    'Apparel',      1,  55.00,   0.00,  5.50, 'refunded',  'Europe',        'cash',          'EUR'),
('CUST-006', '2025-03-03', 420.00,  378.00, 'wholesale',   'Electronics',  4, 105.00,   0.00, 42.00, 'completed', 'Asia Pacific',  'bank_transfer', 'USD'),
('CUST-007', '2025-03-11',  33.00,   29.70, 'marketplace', 'Books',        3,  11.00,   0.00,  3.30, 'completed', 'North America', 'paypal',        'USD'),
('CUST-003', '2025-03-22', 670.00,  603.00, 'online',      'Electronics',  1, 670.00,   0.00, 67.00, 'completed', 'Asia Pacific',  'credit_card',   'USD'),
('CUST-008', '2025-04-01', 145.00,  130.50, 'in-store',    'Home & Garden',2,  75.00,   5.00, 14.50, 'completed', 'Europe',        'credit_card',   'EUR'),
('CUST-005', '2025-04-15',  22.00,   19.80, 'marketplace', 'Books',        2,  11.00,   0.00,  2.20, 'cancelled', 'North America', 'paypal',        'USD'),
('CUST-009', '2025-05-07', 890.00,  801.00, 'wholesale',   'Electronics',  5, 178.00,   0.00, 89.00, 'completed', 'North America', 'bank_transfer', 'USD'),
('CUST-010', '2025-05-23',  60.00,   54.00, 'online',      'Food & Bev',   6,  10.00,   0.00,  6.00, 'completed', 'Europe',        'credit_card',   'EUR'),
('CUST-004', '2025-06-10', 130.00,  117.00, 'in-store',    'Apparel',      2,  65.00,   0.00, 13.00, 'completed', 'Europe',        'cash',          'EUR'),
('CUST-006', '2025-06-18', 275.00,  247.50, 'online',      'Home & Garden',1, 275.00,   0.00, 27.50, 'completed', 'Asia Pacific',  'credit_card',   'USD'),
('CUST-007', '2025-07-04',  48.00,   43.20, 'marketplace', 'Apparel',      2,  24.00,   0.00,  4.80, 'completed', 'North America', 'paypal',        'USD'),
('CUST-001', '2025-07-20', 500.00,  450.00, 'wholesale',   'Electronics',  5, 100.00,   0.00, 50.00, 'completed', 'North America', 'bank_transfer', 'USD'),
('CUST-008', '2025-08-05',  95.00,   85.50, 'online',      'Food & Bev',   9,  10.00,   0.00,  9.50, 'completed', 'Europe',        'credit_card',   'EUR'),
('CUST-010', '2025-08-30', 360.00,  324.00, 'in-store',    'Electronics',  2, 180.00,   0.00, 36.00, 'completed', 'Europe',        'cash',          'EUR');
