# Database Schema Guide - Inventory Management System

## Table of Contents
1. [Overview](#overview)
2. [Entity Relationship Diagram](#entity-relationship-diagram)
3. [Table Schemas](#table-schemas)
4. [Indexes & Performance](#indexes--performance)
5. [SQL Queries](#sql-queries)
6. [Data Seeding](#data-seeding)
7. [Backup & Recovery](#backup--recovery)

---

## Overview

**Database Type:** PostgreSQL 15

**Database Name:** `InvMgmtDb`

**Default Port:** 5432 (5433 external)

**Connection String:**
```
Host=db;Port=5432;Database=InvMgmtDb;Username=postgres;Password=<password>
```

**Total Tables:** 19

**Total Entities:** 19

---

## Entity Relationship Diagram

```
┌─────────────────────────────────────────────────────────────────────────┐
│                      USER MANAGEMENT MODULE                             │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  ┌─────────────────┐         ┌──────────────────┐                       │
│  │   Users (PK:Id)│         │  Roles (PK:Id)   │                       │
│  ├─────────────────┤         ├──────────────────┤                       │
│  │ Id              │◄────────┤ Id               │                       │
│  │ Username        │    1:N  │ Name             │                       │
│  │ Email (Unique)  │         └──────────────────┘                       │
│  │ PasswordHash    │                                                    │
│  │ DepartmentId    │──────────────┐                                     │
│  │ Designation     │              │                                     │
│  │ Role            │              ▼                                     │
│  │ IsActive        │    ┌──────────────────────┐                        │
│  │ IsApproved      │    │ Departments (PK:Id)  │                        │
│  │ CreatedAt       │    ├──────────────────────┤                        │
│  │ UpdatedAt       │    │ Id                   │                        │
│  └─────────────────┘    │ Name                 │                        │
│          ▲              └──────────────────────┘                        │
│          │ 1:N                                                          │
│          │          ┌──────────────────────┐                            │
│          └──────────┤ UserRole (PK:U,R)    │                            │
│                     ├──────────────────────┤                            │
│                     │ UserId (FK)          │                            │
│                     │ RoleId (FK)          │                            │
│                     │ AssignedDate         │                            │
│                     └──────────────────────┘                            │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────┐
│                      INVENTORY MODULE                                   │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  ┌──────────────────────┐      ┌──────────────────────┐                 │
│  │ Categories (PK:Id)   │      │ Items (PK:Id)        │                 │
│  ├──────────────────────┤      ├──────────────────────┤                 │
│  │ Id                   │◄─────┤ Id                   │                 │
│  │ Name                 │  1:N │ Name (Unique)        │                 │
│  │ Description          │      │ CategoryId (FK)      │                 │
│  │ CreatedAt            │      │ Description          │                 │
│  └──────────────────────┘      │ UnitPrice            │                 │
│                                │ IsActive             │                 │
│                                │ CreatedAt            │                 │
│                                └──────────────────────┘                 │
│                                         │                               │
│                                         │ 1:1                           │
│                                         ▼                               │
│                          ┌──────────────────────────────┐               │
│                          │ InventoryStock (PK:Id)       │               │
│                          ├──────────────────────────────┤               │
│                          │ Id                           │               │
│                          │ ItemId (FK - Unique)         │               │
│                          │ TotalQuantity                │               │
│                          │ AvailableQuantity            │               │
│                          │ ReorderLevel                 │               │
│                          │ UpdatedAt                    │               │
│                          └──────────────────────────────┘               │
│                                                                          │
│  ┌──────────────────────┐         ┌───────────────────┐                 │
│  │ RoleItemLimit        │         │ Items.Id (FK)     │                 │
│  ├──────────────────────┤         └───────────────────┘                 │
│  │ RoleId (FK)          │◄──────────┬─────────────────┐                 │
│  │ ItemId (FK)          │           │                 │                 │
│  │ MaxQuantity          │           ▼                 ▼                 │
│  └──────────────────────┘           (...many-to-many relationship...)   │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────┐
│                      REQUEST WORKFLOW MODULE                            │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  ┌──────────────────────┐      ┌──────────────────────┐                 │
│  │ Requests (PK:Id)     │      │ RequestItems (PK:Id) │                 │
│  ├──────────────────────┤      ├──────────────────────┤                 │
│  │ Id                   │◄─────┤ Id                   │                 │
│  │ UserId (FK)          │  1:N │ RequestId (FK)       │                 │
│  │ Status               │      │ ItemId (FK)          │                 │
│  │ CreatedAt            │      │ QuantityRequested    │                 │
│  │ UpdatedAt            │      │ QuantityApproved     │                 │
│  └──────────────────────┘      │ QuantityIssued       │                 │
│           ▲                    │ QuantityReceived     │                 │
│           │ 1:N                │ Status               │                 │
│           │                    │ UpdatedAt            │                 │
│           │                    └──────────────────────┘                 │
│           │                             │                               │
│           │                             │ 1:N                           │
│           │                             ▼                               │
│  ┌────────────────────┐    ┌──────────────────────────┐                 │
│  │ ApprovalLog        │    │ IssueLog                 │                 │
│  ├────────────────────┤    ├──────────────────────────┤                 │
│  │ Id                 │    │ Id                       │                 │
│  │ RequestId (FK)     │    │ RequestItemId (FK)       │                 │
│  │ ApprovedBy (FK)    │    │ IssuedQuantity           │                 │
│  │ Status             │    │ IssuedAt                 │                 │
│  │ Comments           │    │ IssuedBy (UserId)        │                 │
│  │ ApprovedAt         │    └──────────────────────────┘                 │
│  └────────────────────┘                                                 │
│                              ┌──────────────────────────┐                │
│                              │ ReceivedLog              │                │
│                              ├──────────────────────────┤                │
│                              │ Id                       │                │
│                              │ RequestItemId (FK)       │                │
│                              │ ReceivedQuantity         │                │
│                              │ ReceivedAt               │                │
│                              └──────────────────────────┘                │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────┐
│                      REGISTRATION MODULE                                │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  ┌──────────────────────────────┐                                       │
│  │ RegistrationRequest (PK:Id)  │                                       │
│  ├──────────────────────────────┤                                       │
│  │ Id                           │                                       │
│  │ Username                     │                                       │
│  │ Email                        │                                       │
│  │ DepartmentId                 │                                       │
│  │ Designation                  │                                       │
│  │ Status (Pending,Approved,    │                                       │
│  │         Rejected)            │                                       │
│  │ ApprovedBy (UserId)          │                                       │
│  │ CreatedAt                    │                                       │
│  └──────────────────────────────┘                                       │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────┐
│                      PERSONNEL MODULE                                   │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  ┌──────────────────────────────┐                                       │
│  │ Personnel (PK:Id)            │                                       │
│  ├──────────────────────────────┤                                       │
│  │ Id                           │                                       │
│  │ Name                         │                                       │
│  │ Email (Unique)               │                                       │
│  │ Phone                        │                                       │
│  │ Designation                  │                                       │
│  │ Department                   │                                       │
│  │ PhotoUrl                     │                                       │
│  │ DateOfBirth                  │                                       │
│  │ Address                      │                                       │
│  │ CreatedAt                    │                                       │
│  │ UpdatedAt                    │                                       │
│  └──────────────────────────────┘                                       │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────┐
│                      BILLING MODULE                                     │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  ┌──────────────────────┐      ┌──────────────────────┐                 │
│  │ Bills (PK:Id)        │      │ BillItems (PK:Id)    │                 │
│  ├──────────────────────┤      ├──────────────────────┤                 │
│  │ Id                   │◄─────┤ Id                   │                 │
│  │ BillNo (Unique)      │  1:N │ BillId (FK)          │                 │
│  │ CreatedByUserId (FK) │      │ ItemId (FK)          │                 │
│  │ BillDate             │      │ Quantity             │                 │
│  │ TotalAmount          │      │ UnitPrice            │                 │
│  │ CreatedAt            │      └──────────────────────┘                 │
│  └──────────────────────┘                                               │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────┐
│                      AUDIT & LOGGING MODULE                             │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  ┌──────────────────────┐                                               │
│  │ AuditLog (PK:Id)     │                                               │
│  ├──────────────────────┤                                               │
│  │ Id                   │                                               │
│  │ Entity               │                                               │
│  │ Action               │                                               │
│  │ Changes (JSON)       │                                               │
│  │ UserId (FK)          │                                               │
│  │ Timestamp            │                                               │
│  └──────────────────────┘                                               │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## Table Schemas

### 1. Users Table
```sql
CREATE TABLE "Users" (
    "Id" serial PRIMARY KEY,
    "Username" character varying(255) NOT NULL,
    "Email" character varying(255) NOT NULL UNIQUE,
    "PasswordHash" text NOT NULL,
    "DepartmentId" integer,
    "Designation" character varying(255),
    "Role" character varying(50),
    "IsActive" boolean DEFAULT true,
    "IsApproved" boolean DEFAULT false,
    "CreatedAt" timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" timestamp with time zone,
    FOREIGN KEY ("DepartmentId") REFERENCES "Departments" ("Id")
);

CREATE INDEX "idx_users_email" ON "Users" ("Email");
CREATE INDEX "idx_users_role" ON "Users" ("Role");
```

---

### 2. Roles Table
```sql
CREATE TABLE "Roles" (
    "Id" serial PRIMARY KEY,
    "Name" character varying(100) NOT NULL UNIQUE
);

INSERT INTO "Roles" ("Name") VALUES ('User'), ('Issuer'), ('Admin');
```

---

### 3. Departments Table
```sql
CREATE TABLE "Departments" (
    "Id" serial PRIMARY KEY,
    "Name" character varying(100) NOT NULL UNIQUE
);

INSERT INTO "Departments" ("Name") VALUES 
    ('Admin'),
    ('IT'),
    ('HR'),
    ('Finance');
```

---

### 4. Categories Table
```sql
CREATE TABLE "Categories" (
    "Id" serial PRIMARY KEY,
    "Name" character varying(255) NOT NULL,
    "Description" text,
    "CreatedAt" timestamp with time zone DEFAULT CURRENT_TIMESTAMP
);

INSERT INTO "Categories" ("Name") VALUES 
    ('Stationary'),
    ('IT Related'),
    ('HouseKeeping');
```

---

### 5. Items Table
```sql
CREATE TABLE "Items" (
    "Id" serial PRIMARY KEY,
    "Name" character varying(255) NOT NULL UNIQUE,
    "CategoryId" integer NOT NULL,
    "Description" text,
    "UnitPrice" numeric(10, 2) DEFAULT 0,
    "IsActive" boolean DEFAULT true,
    "CreatedAt" timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY ("CategoryId") REFERENCES "Categories" ("Id"),
    CONSTRAINT "fk_items_category" FOREIGN KEY ("CategoryId")
        REFERENCES "Categories" ("Id") ON DELETE CASCADE
);

CREATE INDEX "idx_items_category" ON "Items" ("CategoryId");
CREATE INDEX "idx_items_name" ON "Items" ("Name");
```

---

### 6. InventoryStock Table
```sql
CREATE TABLE "InventoryStock" (
    "Id" serial PRIMARY KEY,
    "ItemId" integer NOT NULL UNIQUE,
    "TotalQuantity" integer DEFAULT 0,
    "AvailableQuantity" integer DEFAULT 0,
    "ReorderLevel" integer DEFAULT 0,
    "UpdatedAt" timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY ("ItemId") REFERENCES "Items" ("Id"),
    CONSTRAINT "fk_stock_item" FOREIGN KEY ("ItemId")
        REFERENCES "Items" ("Id") ON DELETE CASCADE
);

CREATE INDEX "idx_stock_item" ON "InventoryStock" ("ItemId");
```

---

### 7. Requests Table
```sql
CREATE TABLE "Requests" (
    "Id" serial PRIMARY KEY,
    "UserId" integer NOT NULL,
    "Status" character varying(50) DEFAULT 'Pending',
    "CreatedAt" timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" timestamp with time zone,
    FOREIGN KEY ("UserId") REFERENCES "Users" ("Id")
);

CREATE INDEX "idx_requests_user" ON "Requests" ("UserId");
CREATE INDEX "idx_requests_status" ON "Requests" ("Status");
CREATE INDEX "idx_requests_created" ON "Requests" ("CreatedAt");
```

---

### 8. RequestItems Table
```sql
CREATE TABLE "RequestItems" (
    "Id" serial PRIMARY KEY,
    "RequestId" integer NOT NULL,
    "ItemId" integer NOT NULL,
    "QuantityRequested" integer NOT NULL,
    "QuantityApproved" integer DEFAULT 0,
    "QuantityIssued" integer DEFAULT 0,
    "QuantityReceived" integer DEFAULT 0,
    "Status" character varying(50) DEFAULT 'Pending',
    "UpdatedAt" timestamp with time zone,
    FOREIGN KEY ("RequestId") REFERENCES "Requests" ("Id"),
    FOREIGN KEY ("ItemId") REFERENCES "Items" ("Id")
);

CREATE INDEX "idx_request_items_request" ON "RequestItems" ("RequestId");
CREATE INDEX "idx_request_items_item" ON "RequestItems" ("ItemId");
CREATE INDEX "idx_request_items_status" ON "RequestItems" ("Status");
```

---

### 9. ApprovalLog Table
```sql
CREATE TABLE "ApprovalLog" (
    "Id" serial PRIMARY KEY,
    "RequestId" integer NOT NULL,
    "ApprovedBy" integer,
    "Status" character varying(50),
    "Comments" text,
    "ApprovedAt" timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY ("RequestId") REFERENCES "Requests" ("Id"),
    FOREIGN KEY ("ApprovedBy") REFERENCES "Users" ("Id")
);

CREATE INDEX "idx_approval_request" ON "ApprovalLog" ("RequestId");
```

---

### 10. IssueLog Table
```sql
CREATE TABLE "IssueLog" (
    "Id" serial PRIMARY KEY,
    "RequestItemId" integer NOT NULL,
    "IssuedQuantity" integer NOT NULL,
    "IssuedAt" timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    "IssuedBy" integer,
    FOREIGN KEY ("RequestItemId") REFERENCES "RequestItems" ("Id"),
    FOREIGN KEY ("IssuedBy") REFERENCES "Users" ("Id")
);

CREATE INDEX "idx_issue_request_item" ON "IssueLog" ("RequestItemId");
```

---

### 11. ReceivedLog Table
```sql
CREATE TABLE "ReceivedLog" (
    "Id" serial PRIMARY KEY,
    "RequestItemId" integer NOT NULL,
    "ReceivedQuantity" integer NOT NULL,
    "ReceivedAt" timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY ("RequestItemId") REFERENCES "RequestItems" ("Id")
);

CREATE INDEX "idx_received_request_item" ON "ReceivedLog" ("RequestItemId");
```

---

### 12. Personnel Table
```sql
CREATE TABLE "Personnel" (
    "Id" serial PRIMARY KEY,
    "Name" character varying(255) NOT NULL,
    "Email" character varying(255) NOT NULL UNIQUE,
    "Phone" character varying(20),
    "Designation" character varying(255),
    "Department" character varying(255),
    "PhotoUrl" text,
    "DateOfBirth" date,
    "Address" text,
    "CreatedAt" timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" timestamp with time zone
);

CREATE INDEX "idx_personnel_email" ON "Personnel" ("Email");
```

---

### 13. Bills Table
```sql
CREATE TABLE "Bills" (
    "Id" serial PRIMARY KEY,
    "BillNo" character varying(50) NOT NULL UNIQUE,
    "CreatedByUserId" integer NOT NULL,
    "BillDate" date NOT NULL,
    "TotalAmount" numeric(10, 2) DEFAULT 0,
    "CreatedAt" timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY ("CreatedByUserId") REFERENCES "Users" ("Id")
);

CREATE INDEX "idx_bills_bill_no" ON "Bills" ("BillNo");
CREATE INDEX "idx_bills_created_by" ON "Bills" ("CreatedByUserId");
CREATE INDEX "idx_bills_created_at" ON "Bills" ("CreatedAt");
```

---

### 14. BillItems Table
```sql
CREATE TABLE "BillItems" (
    "Id" serial PRIMARY KEY,
    "BillId" integer NOT NULL,
    "ItemId" integer NOT NULL,
    "Quantity" integer NOT NULL,
    "UnitPrice" numeric(10, 2) NOT NULL,
    FOREIGN KEY ("BillId") REFERENCES "Bills" ("Id"),
    FOREIGN KEY ("ItemId") REFERENCES "Items" ("Id")
);

CREATE INDEX "idx_bill_items_bill" ON "BillItems" ("BillId");
CREATE INDEX "idx_bill_items_item" ON "BillItems" ("ItemId");
```

---

### 15. RegistrationRequest Table
```sql
CREATE TABLE "RegistrationRequest" (
    "Id" serial PRIMARY KEY,
    "Username" character varying(255) NOT NULL,
    "Email" character varying(255) NOT NULL,
    "DepartmentId" integer,
    "Designation" character varying(255),
    "Status" character varying(50) DEFAULT 'Pending',
    "ApprovedBy" integer,
    "CreatedAt" timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY ("DepartmentId") REFERENCES "Departments" ("Id"),
    FOREIGN KEY ("ApprovedBy") REFERENCES "Users" ("Id")
);

CREATE INDEX "idx_registration_status" ON "RegistrationRequest" ("Status");
```

---

### 16. AuditLog Table
```sql
CREATE TABLE "AuditLog" (
    "Id" serial PRIMARY KEY,
    "Entity" character varying(255),
    "Action" character varying(50),
    "Changes" jsonb,
    "UserId" integer,
    "Timestamp" timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY ("UserId") REFERENCES "Users" ("Id")
);

CREATE INDEX "idx_audit_entity" ON "AuditLog" ("Entity");
CREATE INDEX "idx_audit_timestamp" ON "AuditLog" ("Timestamp");
```

---

### 17. RoleItemLimit Table
```sql
CREATE TABLE "RoleItemLimit" (
    "RoleId" integer NOT NULL,
    "ItemId" integer NOT NULL,
    "MaxQuantity" integer DEFAULT 0,
    PRIMARY KEY ("RoleId", "ItemId"),
    FOREIGN KEY ("RoleId") REFERENCES "Roles" ("Id"),
    FOREIGN KEY ("ItemId") REFERENCES "Items" ("Id")
);

CREATE INDEX "idx_role_limit_role" ON "RoleItemLimit" ("RoleId");
CREATE INDEX "idx_role_limit_item" ON "RoleItemLimit" ("ItemId");
```

---

### 18. UserRole Table
```sql
CREATE TABLE "UserRole" (
    "UserId" integer NOT NULL,
    "RoleId" integer NOT NULL,
    "AssignedDate" timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY ("UserId", "RoleId"),
    FOREIGN KEY ("UserId") REFERENCES "Users" ("Id"),
    FOREIGN KEY ("RoleId") REFERENCES "Roles" ("Id")
);
```

---

### 19. ErrorViewModel Table (Optional)
```sql
CREATE TABLE "ErrorViewModel" (
    "RequestId" character varying(255) PRIMARY KEY,
    "ShowRequestId" boolean DEFAULT false
);
```

---

## Indexes & Performance

### Critical Indexes for Query Performance

```sql
-- User lookups
CREATE INDEX "idx_users_email" ON "Users" ("Email") WHERE "IsActive" = true;
CREATE INDEX "idx_users_role" ON "Users" ("Role");

-- Inventory lookups
CREATE INDEX "idx_items_name" ON "Items" ("Name");
CREATE INDEX "idx_items_category" ON "Items" ("CategoryId");
CREATE INDEX "idx_stock_item" ON "InventoryStock" ("ItemId");

-- Request workflow
CREATE INDEX "idx_requests_user" ON "Requests" ("UserId");
CREATE INDEX "idx_requests_status" ON "Requests" ("Status");
CREATE INDEX "idx_request_items_request" ON "RequestItems" ("RequestId");
CREATE INDEX "idx_request_items_status" ON "RequestItems" ("Status");

-- Personnel
CREATE INDEX "idx_personnel_email" ON "Personnel" ("Email");

-- Bills
CREATE INDEX "idx_bills_created_at" ON "Bills" ("CreatedAt");
CREATE INDEX "idx_bills_bill_no" ON "Bills" ("BillNo");

-- Audit
CREATE INDEX "idx_audit_timestamp" ON "AuditLog" ("Timestamp" DESC);
```

### Unique Constraints

```sql
ALTER TABLE "Users" ADD CONSTRAINT "uq_users_email" UNIQUE ("Email");
ALTER TABLE "Items" ADD CONSTRAINT "uq_items_name" UNIQUE ("Name");
ALTER TABLE "Personnel" ADD CONSTRAINT "uq_personnel_email" UNIQUE ("Email");
ALTER TABLE "Bills" ADD CONSTRAINT "uq_bills_billno" UNIQUE ("BillNo");
```

---

## SQL Queries

### Common Queries

#### 1. Get All Pending Requests
```sql
SELECT r."Id", r."UserId", r."Status", r."CreatedAt", 
       u."Username", u."Email",
       COUNT(ri."Id") as "ItemCount"
FROM "Requests" r
JOIN "Users" u ON r."UserId" = u."Id"
LEFT JOIN "RequestItems" ri ON r."Id" = ri."RequestId"
WHERE r."Status" = 'Pending'
GROUP BY r."Id", u."Id"
ORDER BY r."CreatedAt" DESC;
```

#### 2. Get Request Details with Items
```sql
SELECT r."Id", r."Status", r."CreatedAt",
       ri."ItemId", i."Name", i."UnitPrice",
       ri."QuantityRequested", ri."QuantityApproved", 
       ri."QuantityIssued", ri."Status" as "ItemStatus"
FROM "Requests" r
LEFT JOIN "RequestItems" ri ON r."Id" = ri."RequestId"
LEFT JOIN "Items" i ON ri."ItemId" = i."Id"
WHERE r."Id" = 42
ORDER BY ri."Id";
```

#### 3. Get Inventory Summary
```sql
SELECT 
    c."Name" as "Category",
    i."Name" as "Item",
    s."TotalQuantity",
    s."AvailableQuantity",
    s."ReorderLevel",
    (s."AvailableQuantity" <= s."ReorderLevel") as "NeedsReorder"
FROM "Items" i
JOIN "Categories" c ON i."CategoryId" = c."Id"
LEFT JOIN "InventoryStock" s ON i."Id" = s."ItemId"
WHERE i."IsActive" = true
ORDER BY c."Name", i."Name";
```

#### 4. Get User Request History
```sql
SELECT r."Id", r."Status", r."CreatedAt", r."UpdatedAt",
       COUNT(ri."Id") as "ItemCount",
       SUM(ri."QuantityRequested") as "TotalQuantity"
FROM "Requests" r
LEFT JOIN "RequestItems" ri ON r."Id" = ri."RequestId"
WHERE r."UserId" = 5
GROUP BY r."Id"
ORDER BY r."CreatedAt" DESC
LIMIT 10;
```

#### 5. Get Bills for Date Range
```sql
SELECT b."Id", b."BillNo", b."BillDate", b."TotalAmount",
       u."Username", u."Email",
       COUNT(bi."Id") as "ItemCount"
FROM "Bills" b
JOIN "Users" u ON b."CreatedByUserId" = u."Id"
LEFT JOIN "BillItems" bi ON b."Id" = bi."BillId"
WHERE b."BillDate" BETWEEN '2026-06-01' AND '2026-06-30'
GROUP BY b."Id", u."Id"
ORDER BY b."BillDate" DESC;
```

#### 6. Get Stock Movement History
```sql
SELECT 
    i."Name" as "Item",
    il."IssuedQuantity",
    il."IssuedAt",
    rl."ReceivedQuantity",
    rl."ReceivedAt"
FROM "IssueLog" il
LEFT JOIN "RequestItems" ri ON il."RequestItemId" = ri."Id"
LEFT JOIN "Items" i ON ri."ItemId" = i."Id"
LEFT JOIN "ReceivedLog" rl ON ri."Id" = rl."RequestItemId"
ORDER BY il."IssuedAt" DESC;
```

#### 7. Get Personnel by Department
```sql
SELECT "Name", "Email", "Phone", "Designation", "Department"
FROM "Personnel"
WHERE "Department" = 'IT'
ORDER BY "Name";
```

#### 8. Get Audit Trail
```sql
SELECT a."Entity", a."Action", a."Changes", 
       u."Username", a."Timestamp"
FROM "AuditLog" a
LEFT JOIN "Users" u ON a."UserId" = u."Id"
WHERE a."Timestamp" >= NOW() - INTERVAL '7 days'
ORDER BY a."Timestamp" DESC;
```

---

## Data Seeding

### Initial Seed Data

```sql
-- Insert Roles
INSERT INTO "Roles" ("Name") VALUES 
    ('User'),
    ('Issuer'),
    ('Admin');

-- Insert Departments
INSERT INTO "Departments" ("Name") VALUES 
    ('Admin'),
    ('IT'),
    ('HR'),
    ('Finance');

-- Insert Categories
INSERT INTO "Categories" ("Name") VALUES 
    ('Stationary'),
    ('IT Related'),
    ('HouseKeeping');

-- Insert Admin User
INSERT INTO "Users" 
    ("Username", "Email", "PasswordHash", "DepartmentId", "Designation", "Role", "IsActive", "IsApproved")
VALUES 
    ('System Admin', 'admin@gmail.com', '$2a$11$...', 1, 'System Administrator', 'ADMIN', true, true);

-- Insert Sample Items
INSERT INTO "Items" ("Name", "CategoryId", "Description", "UnitPrice")
VALUES 
    ('Laptop', 2, 'Dell Laptop 14-inch', 50000),
    ('Monitor', 2, '22-inch LED Monitor', 15000),
    ('Pen', 1, 'Ballpoint Pen', 10),
    ('Notebook', 1, 'A4 Notebook', 50);

-- Insert Stock
INSERT INTO "InventoryStock" ("ItemId", "TotalQuantity", "AvailableQuantity", "ReorderLevel")
VALUES 
    (1, 10, 10, 2),
    (2, 15, 15, 3),
    (3, 200, 200, 50),
    (4, 150, 150, 30);
```

---

## Backup & Recovery

### Backup Commands

#### PostgreSQL Backup (Full)
```bash
pg_dump -h localhost -U postgres -d InvMgmtDb -F c > InvMgmtDb_backup.dump
```

#### PostgreSQL Backup (Plain SQL)
```bash
pg_dump -h localhost -U postgres -d InvMgmtDb > InvMgmtDb_backup.sql
```

#### Docker Backup
```bash
docker exec invmgmt_db pg_dump -U postgres InvMgmtDb > backup.sql
```

### Restore Commands

#### From Custom Format
```bash
pg_restore -h localhost -U postgres -d InvMgmtDb -F c InvMgmtDb_backup.dump
```

#### From SQL File
```bash
psql -h localhost -U postgres -d InvMgmtDb -f InvMgmtDb_backup.sql
```

#### Docker Restore
```bash
docker exec -i invmgmt_db psql -U postgres -d InvMgmtDb < backup.sql
```

### Backup Strategy

**Frequency:** Daily at 2:00 AM

**Retention:** 30 days

**Location:** `/backups/invmgmt/`

**Automated Script:**
```bash
#!/bin/bash
BACKUP_DIR="/backups/invmgmt"
DATE=$(date +%Y%m%d_%H%M%S)
pg_dump -h db -U postgres InvMgmtDb | gzip > $BACKUP_DIR/InvMgmtDb_$DATE.sql.gz
find $BACKUP_DIR -name "*.sql.gz" -mtime +30 -delete
```

---

## Performance Monitoring

### Check Index Usage
```sql
SELECT schemaname, tablename, indexname, idx_scan, idx_tup_read, idx_tup_fetch
FROM pg_stat_user_indexes
ORDER BY idx_scan DESC;
```

### Check Table Size
```sql
SELECT schemaname, tablename, pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename)) as size
FROM pg_tables
WHERE schemaname NOT IN ('pg_catalog', 'information_schema')
ORDER BY pg_total_relation_size(schemaname||'.'||tablename) DESC;
```

### Check Slow Queries
```sql
SELECT query, calls, total_time, mean_time
FROM pg_stat_statements
ORDER BY mean_time DESC
LIMIT 10;
```

### Check Database Size
```sql
SELECT pg_database.datname, pg_size_pretty(pg_database_size(pg_database.datname))
FROM pg_database
ORDER BY pg_database_size(pg_database.datname) DESC;
```

---

## Maintenance Tasks

### Analyze & Vacuum
```sql
-- Analyze query planner
ANALYZE;

-- Vacuum dead tuples
VACUUM;

-- Full vacuum (downtime required)
VACUUM FULL;
```

### Reindex
```sql
-- Reindex specific index
REINDEX INDEX idx_users_email;

-- Reindex table
REINDEX TABLE "Users";

-- Reindex database
REINDEX DATABASE InvMgmtDb;
```

---

Last Updated: **June 5, 2026**
