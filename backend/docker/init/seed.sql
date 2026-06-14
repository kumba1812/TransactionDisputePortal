-- Seed Transactions and Disputes tables for TransactionDisputePortal

CREATE TABLE IF NOT EXISTS "Transactions" (
	"Id" serial PRIMARY KEY,
	"CustomerId" int NOT NULL,
	"TransactionId" varchar(100) NOT NULL,
	"Amount" numeric(18,2) NOT NULL,
	"Description" varchar(500),
	"TransactionDate" timestamp without time zone,
	"Merchant" varchar(200),
	"Category" varchar(100),
	"Status" int NOT NULL,
	"CreatedAt" timestamp without time zone DEFAULT now()
);

CREATE TABLE IF NOT EXISTS "Disputes" (
	"Id" serial PRIMARY KEY,
	"TransactionId" int NOT NULL,
	"CustomerId" int NOT NULL,
	"Reason" varchar(100) NOT NULL,
	"Description" varchar(1000) NOT NULL,
	"Status" int NOT NULL,
	"CreatedAt" timestamp without time zone DEFAULT now(),
	"ResolvedAt" timestamp without time zone,
	"ResolutionNotes" varchar(1000),
	"RefundAmount" numeric(18,2)
);

-- Insert sample transactions
INSERT INTO "Transactions" ("CustomerId","TransactionId","Amount","Description","TransactionDate","Merchant","Category","Status") VALUES
(1,'TXN001',125.50,'Online Purchase', now() - interval '10 days','Amazon','Shopping',0),
(1,'TXN002',89.99,'Electronics', now() - interval '5 days','Best Buy','Electronics',0),
(1,'TXN003',45.00,'Restaurant', now() - interval '2 days','Pizza Hut','Dining',0),
(1,'TXN004',200.00,'Flight Booking', now() - interval '15 days','Delta Airlines','Travel',0)
ON CONFLICT DO NOTHING;

-- Insert sample disputes
INSERT INTO "Disputes" ("TransactionId","CustomerId","Reason","Description","Status","CreatedAt","RefundAmount") VALUES
(1,1,'Unauthorized','I did not authorize this purchase',1, now() - interval '3 days',125.50),
(4,1,'Duplicate Charge','This flight was charged twice',2, now() - interval '20 days',200.00)
ON CONFLICT DO NOTHING;
