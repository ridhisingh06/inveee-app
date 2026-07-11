# Database Relationships - Visual Guide

## All Relationships in Your App

### **Relationship 1: User ↔ Department (One-to-Many)**

```
ONE Department has MANY Users
ONE User belongs to ONE Department

Department         User
┌───────────┐     ┌──────────────┐
│ Id = 1    │────▶│ Id = 1       │
│ Name=Admin│     │ DepartmentId │─┐ Points to Dept 1
└───────────┘     └──────────────┘ │
                                    │
                  ┌──────────────┐  │ Both point
                  │ Id = 2       │  │ to Department 1
                  │ DepartmentId │─┴─┐ (Admin)
                  └──────────────┘    │
                                      │
                  ┌──────────────┐    │
                  │ Id = 3       │    │
                  │ DepartmentId │────┘
                  └──────────────┘

SQL View:
Department Table:
┌────┬─────────┐
| Id | Name    |
├────┼─────────┤
| 1  | Admin   |  ◄─ Users 1, 2, 3 all point here
| 2  | IT      |
| 3  | HR      |
└────┴─────────┘

User Table:
┌────┬──────────┬──────────────┐
| Id | Username | DepartmentId |
├────┼──────────┼──────────────┤
| 1  | Admin    | 1            |  All DepartmentId = 1
| 2  | John     | 1            |  means all work in
| 3  | Sarah    | 1            |  Admin department
└────┴──────────┴──────────────┘
```

---

### **Relationship 2: User ↔ Request (One-to-Many)**

```
ONE User creates MANY Requests
ONE Request belongs to ONE User

User                Request
┌──────────┐       ┌────────────┐
│ Id = 1   │──────▶│ Id = 42    │
│ Admin    │       │ UserId = 1 │  Request 42 by User 1
└──────────┘       └────────────┘
      │
      │            ┌────────────┐
      └───────────▶│ Id = 43    │
                   │ UserId = 1 │  Request 43 by User 1
                   └────────────┘
      
                   ┌────────────┐
                   │ Id = 44    │
                   │ UserId = 1 │  Request 44 by User 1
                   └────────────┘

Timeline:
User 1 creates request 42
  ↓ (later)
User 1 creates request 43
  ↓ (later)
User 1 creates request 44

All linked by UserId = 1
```

---

### **Relationship 3: Request ↔ RequestItem (One-to-Many)**

```
ONE Request has MANY RequestItems
ONE RequestItem belongs to ONE Request

Request            RequestItem
┌────────┐        ┌─────────────────┐
│ Id=42  │───────▶│ Id = 100        │
│        │        │ RequestId = 42  │  Item 5, Qty 10
└────────┘        │ ItemId = 5      │
      │            └─────────────────┘
      │
      │           ┌─────────────────┐
      └──────────▶│ Id = 101        │
                  │ RequestId = 42  │  Item 8, Qty 5
                  │ ItemId = 8      │
                  └─────────────────┘
      
                  ┌─────────────────┐
                  │ Id = 102        │
                  │ RequestId = 42  │  Item 15, Qty 3
                  │ ItemId = 15     │
                  └─────────────────┘

One Request can ask for multiple items
```

---

### **Relationship 4: Request ↔ Category (One-to-Many)**

```
ONE Category has MANY Requests
ONE Request belongs to ONE Category

Category           Request
┌────────────────┐  ┌──────────────┐
│ Id = 1         │ │ Id = 42       │
│ Stationary     │─▶│ CategoryId=1  │
└────────────────┘  └──────────────┘
      │
      │             ┌──────────────┐
      └────────────▶│ Id = 43      │
                    │ CategoryId=1 │
                    └──────────────┘
      
                    ┌──────────────┐
                    │ Id = 44      │
                    │ CategoryId=1 │
                    └──────────────┘

Multiple requests for same category
```

---

## Complete Data Flow: Creating a Request

### **What happens when User creates a Request:**

```
Step 1: Frontend sends
┌────────────────────────────────────────────┐
│ POST /api/requests/create                  │
│ {                                          │
│   "items": [                               │
│     { "itemId": 5, "quantity": 10 },      │
│     { "itemId": 8, "quantity": 5 }        │
│   ]                                        │
│ }                                          │
└────────────────────────────────────────────┘
                    ↓
Step 2: Backend processes
┌────────────────────────────────────────────┐
│ RequestService.CreateAsync()               │
│ - Get current user (UserId = 1)            │
│ - Create new Request                       │
│ - Add to RequestItems for each item        │
└────────────────────────────────────────────┘
                    ↓
Step 3: Database INSERT
┌────────────────────────────────────────────┐
│ INSERT Request                             │
│ Values: (UserId=1, Status='Pending')       │
│ Returns: RequestId = 42                    │
│                                            │
│ INSERT RequestItem 1                       │
│ Values: (RequestId=42, ItemId=5, Qty=10)   │
│ Returns: RequestItemId = 100               │
│                                            │
│ INSERT RequestItem 2                       │
│ Values: (RequestId=42, ItemId=8, Qty=5)    │
│ Returns: RequestItemId = 101               │
└────────────────────────────────────────────┘
                    ↓
Step 4: Database State
┌────────────────────────────────────────────┐
│ Request Table NEW ROW:                     │
│ Id=42, UserId=1, Status='Pending'          │
│                                            │
│ RequestItem Table NEW ROW:                 │
│ Id=100, RequestId=42, ItemId=5, Qty=10     │
│                                            │
│ RequestItem Table NEW ROW:                 │
│ Id=101, RequestId=42, ItemId=8, Qty=5      │
└────────────────────────────────────────────┘
                    ↓
Step 5: Frontend Response
┌────────────────────────────────────────────┐
│ {                                          │
│   "id": 42,                                │
│   "userId": 1,                             │
│   "status": "Pending",                     │
│   "items": [                               │
│     { "itemId": 5, "qty": 10 },           │
│     { "itemId": 8, "qty": 5 }             │
│   ]                                        │
│ }                                          │
└────────────────────────────────────────────┘
```

---

## All Tables Connected

```
                    ┌──────────────────┐
                    │   Department     │
                    │  ┌────────────┐  │
                    │  │ Id (PK)    │  │
                    │  │ Name       │  │
                    │  └────────────┘  │
                    └────────┬─────────┘
                             △
                             │ Foreign Key
                             │ DepartmentId
                             │
    ┌────────────────────────┼────────────────────────┐
    │                        │                        │
    ▼                        ▼                        ▼
┌──────────────┐      ┌──────────────────┐     ┌──────────────┐
│    User      │      │    Category      │     │     Item     │
│ ┌──────────┐ │      │ ┌──────────────┐ │     │ ┌──────────┐ │
│ │ Id (PK)  │ │      │ │ Id (PK)      │ │     │ │ Id (PK)  │ │
│ │ Email    │ │      │ │ Name         │ │     │ │ Name     │ │
│ │ Dept Id  │◄┤      │ └──────────────┘ │     │ │ ...      │ │
│ │ (FK)     │ │      │                  │     │ └──────────┘ │
│ └──────────┘ │      └──────────────────┘     └──────────────┘
└────┬─────────┘             △                        △
     │                       │                        │
     │ Foreign Key           │ Foreign Key            │ Foreign Key
     │ UserId                │ CategoryId             │ ItemId
     │                       │                        │
     ▼                       ▼                        ▼
┌────────────────────────────────────────────────────────┐
│               Request                                  │
│  ┌────────────────────────────────────────────────┐   │
│  │ Id (PK)                                        │   │
│  │ UserId (FK) ────────────────────┐             │   │
│  │ CategoryId (FK) ────────────────┼─────┐       │   │
│  │ Status                          │     │       │   │
│  │ CreatedAt                       │     │       │   │
│  └────────────────────────────────────────────┐   │   │
│                                               │   │   │
│  Links to: User, Category                    │   │   │
└─────────────────────────────────┬─────────────┼───┼───┘
                                  │             │   │
                                  │ Foreign Key │   │
                                  │ RequestId   │   │
                                  │             │   │
                                  ▼             ▼   ▼
                          ┌────────────────────────────┐
                          │    RequestItem             │
                          │  ┌──────────────────────┐  │
                          │  │ Id (PK)              │  │
                          │  │ RequestId (FK)   ────┼──┘
                          │  │ ItemId (FK)  ───────┘
                          │  │ QuantityRequested    │
                          │  │ Status               │
                          │  └──────────────────────┘
                          │                         │
                          │ Links to: Request, Item │
                          └─────────────────────────┘
```

---

## Cascade Delete Behavior

When you delete a User, what happens?

```
User (Id=1) ─┐
             │
             ├──▶ Request (UserId=1)
             │         │
             │         └──▶ RequestItem (RequestId=X)
             │                   │
             │                   └──▶ ApprovalLog
             │                   └──▶ IssueLog
             │                   └──▶ ReceivedLog
             │
             └──▶ Can't delete if requests exist!
                   (Foreign key constraint)

Solution: DELETE IN ORDER
1. DELETE from ApprovalLog (where RequestId...)
2. DELETE from IssueLog (where RequestId...)
3. DELETE from ReceivedLog (where RequestId...)
4. DELETE from RequestItem (where RequestId...)
5. DELETE from Request (where UserId=1)
6. DELETE from User (Id=1)
```

---

## SQL Queries Examples

### **Get user with department name**
```sql
SELECT u."Id", u."Username", u."Email", d."Name"
FROM "User" u
LEFT JOIN "Department" d ON u."DepartmentId" = d."Id"
WHERE u."Email" = 'admin@gmail.com';

Result:
Id | Username     | Email              | Name
---|--------------|--------------------|----- 
1  | System Admin | admin@gmail.com    | Admin
```

### **Get all requests by a user with items**
```sql
SELECT r."Id", r."Status", ri."ItemId", ri."QuantityRequested"
FROM "Request" r
LEFT JOIN "RequestItem" ri ON r."Id" = ri."RequestId"
WHERE r."UserId" = 1
ORDER BY r."CreatedAt" DESC;

Result:
RequestId | Status  | ItemId | Quantity
----------|---------|--------|----------
42        | Pending | 5      | 10
42        | Pending | 8      | 5
43        | Pending | 15     | 3
```

### **Count requests per user**
```sql
SELECT u."Username", COUNT(r."Id") as "RequestCount"
FROM "User" u
LEFT JOIN "Request" r ON u."Id" = r."UserId"
GROUP BY u."Id", u."Username"
ORDER BY "RequestCount" DESC;

Result:
Username     | RequestCount
-------------|-------------
System Admin | 3
John         | 1
Sarah        | 0
```

---

## Key Terms

| Term | Meaning |
|------|---------|
| **Primary Key (PK)** | Unique ID for each row in a table |
| **Foreign Key (FK)** | Column that references PK in another table |
| **JOIN** | Combining data from multiple tables |
| **LEFT JOIN** | Include all rows from left table, matching from right |
| **INNER JOIN** | Only matching rows from both tables |
| **CASCADE** | Delete child records when parent is deleted |
| **NULL** | Empty/no value |
| **Index** | Speed up queries on certain columns |

---

## Your App in One Picture

```
Frontend (Angular)
│ HTTP Request
├─ POST /api/auth/login
│  └─ GET /api/requests
│  └─ POST /api/requests/create
└─ Uses JWT token for auth
        │
        │ HTTP Response
        │
Backend (.NET)
├─ Controllers (receive requests)
│  ├─ AuthController
│  └─ RequestController
├─ Services (business logic)
│  ├─ AuthService
│  └─ RequestService
└─ Repositories (database queries)
   ├─ UserRepository
   └─ RequestRepository
        │
        │ SQL Queries
        │
Database (PostgreSQL)
├─ User table
├─ Department table
├─ Request table
├─ RequestItem table
└─ Category table
   (All connected by Foreign Keys)
```

This is your entire application architecture!
