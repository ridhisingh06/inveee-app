# 🎓 Backend & Database Architecture Explained

## Overview

Your backend is built with:
- **Language**: C# (.NET 10.0)
- **Database**: PostgreSQL
- **ORM**: Entity Framework Core (translates C# to SQL automatically)
- **API Type**: RESTful API (HTTP endpoints)

---

## How the Backend Works (3 Layers)

```
┌─────────────────────────────────────┐
│   Frontend (Angular)                │
│   Makes HTTP requests               │
└──────────────┬──────────────────────┘
               │ HTTP Request
               ↓
┌─────────────────────────────────────┐
│   Backend (C# .NET)                 │
│   - Controllers: receive requests   │
│   - Services: business logic        │
│   - Repositories: database queries  │
└──────────────┬──────────────────────┘
               │ SQL Query
               ↓
┌─────────────────────────────────────┐
│   Database (PostgreSQL)             │
│   - Tables with data                │
│   - Stores everything permanently   │
└─────────────────────────────────────┘
```

---

## How Your App Communicates (Example: Login)

```
User types email & password in Frontend
        ↓
Frontend sends: POST /api/auth/login with email & password
        ↓
Backend Controller receives request
        ↓
AuthService checks if email exists in database
        ↓
Database query: SELECT * FROM "User" WHERE "Email" = 'admin@gmail.com'
        ↓
Database returns user data
        ↓
AuthService compares password (BCrypt verification)
        ↓
If password correct:
  - Generate JWT token
  - Return token to Frontend
  ↓
Frontend stores token
  ↓
Frontend includes token in all future API requests
```

---

## Database Structure: Tables & Relationships

### **1. User Table**

```sql
CREATE TABLE "User" (
    "Id" INT PRIMARY KEY,
    "Username" VARCHAR(255),
    "Email" VARCHAR(255),
    "PasswordHash" VARCHAR(255),
    "DepartmentId" INT,              ← Foreign Key (links to Department)
    "Designation" VARCHAR(255),
    "IsActive" BOOLEAN,
    "IsApproved" BOOLEAN,
    "Role" VARCHAR(50),
    "CreatedAt" TIMESTAMP
);
```

**C# Model:**
```csharp
public class User
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }
    
    // Foreign Key to Department
    public int? DepartmentId { get; set; }
    public Department? Department { get; set; }
    
    public string Designation { get; set; }
    public bool IsActive { get; set; }
    public bool IsApproved { get; set; }
    public string Role { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

### **2. Department Table**

```sql
CREATE TABLE "Department" (
    "Id" INT PRIMARY KEY,
    "Name" VARCHAR(255)
);
```

**Example Data:**
```
Id | Name
---|-------
1  | Admin
2  | IT
3  | HR
4  | Finance
```

**C# Model:**
```csharp
public class Department
{
    public int Id { get; set; }
    public string Name { get; set; }
    public ICollection<User> Users { get; set; }  // One department has many users
}
```

### **3. Request Table**

```sql
CREATE TABLE "Request" (
    "Id" INT PRIMARY KEY,
    "UserId" INT,                     ← Foreign Key (links to User)
    "CategoryId" INT,                 ← Foreign Key (links to Category)
    "Status" VARCHAR(50),
    "CreatedAt" TIMESTAMP,
    "UpdatedAt" TIMESTAMP
);
```

**C# Model:**
```csharp
public class Request
{
    public int Id { get; set; }
    
    // Foreign Key to User
    public int UserId { get; set; }
    public User User { get; set; }
    
    // Foreign Key to Category
    public int? CategoryId { get; set; }
    public Category? Category { get; set; }
    
    public RequestStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // One request has many items
    public ICollection<RequestItem> RequestItems { get; set; }
}
```

### **4. RequestItem Table**

```sql
CREATE TABLE "RequestItem" (
    "Id" INT PRIMARY KEY,
    "RequestId" INT,                  ← Foreign Key (links to Request)
    "ItemId" INT,                     ← Foreign Key (links to Item)
    "QuantityRequested" INT,
    "QuantityApproved" INT,
    "QuantityIssued" INT,
    "Status" VARCHAR(50)
);
```

**C# Model:**
```csharp
public class RequestItem
{
    public int Id { get; set; }
    
    // Foreign Key to Request
    public int RequestId { get; set; }
    public Request Request { get; set; }
    
    // Foreign Key to Item
    public int ItemId { get; set; }
    public Item Item { get; set; }
    
    public int QuantityRequested { get; set; }
    public int QuantityApproved { get; set; }
    public int QuantityIssued { get; set; }
    public RequestItemStatus Status { get; set; }
}
```

### **5. Category Table**

```sql
CREATE TABLE "Category" (
    "Id" INT PRIMARY KEY,
    "Name" VARCHAR(255)
);
```

**Example Data:**
```
Id | Name
---|-------------------
1  | Stationary
2  | IT Related
3  | HouseKeeping
```

---

## Foreign Keys Explained 

### **What is a Foreign Key?**

A **Foreign Key** is a link between tables. It's a column in one table that references the primary key in another table.

### **Example: User → Department**

```
User Table:
┌────┬──────────────┬──────────────────┬──────────────┐
| Id | Username     | Email            | DepartmentId │
├────┼──────────────┼──────────────────┼──────────────┤
| 1  | System Admin | admin@gmail.com  | 1            │ ← Points to Department 1
| 2  | John         | john@gmail.com   | 2            │ ← Points to Department 2
| 3  | Sarah        | sarah@gmail.com  | 3            │ ← Points to Department 3
└────┴──────────────┴──────────────────┴──────────────┘

Department Table:
┌────┬─────────┐
| Id | Name    |
├────┼─────────┤
| 1  | Admin   │ ← User 1 belongs to Admin
| 2  | IT      │ ← User 2 belongs to IT
| 3  | HR      │ ← User 3 belongs to HR
| 4  | Finance │
└────┴─────────┘
```

**In SQL:**
```sql
-- Get user with department name
SELECT u."Username", d."Name" as "DepartmentName"
FROM "User" u
LEFT JOIN "Department" d ON u."DepartmentId" = d."Id"
WHERE u."Id" = 1;

Result:
Username    | DepartmentName
------------|----------------
System Admin | Admin
```

---

## Real-World Example: Creating a Request

### **Step 1: Frontend sends request**
```json
POST /api/requests/create
{
  "items": [
    { "itemId": 5, "quantityRequested": 10 },
    { "itemId": 8, "quantityRequested": 5 }
  ]
}
```

### **Step 2: Backend processes it**

**C# Code (RequestService.cs):**
```csharp
public async Task<Request> CreateRequest(int userId, CreateRequestDto dto)
{
    // 1. Get user from database
    var user = await db.Users.FindAsync(userId);
    
    // 2. Create new request linked to user
    var request = new Request
    {
        UserId = userId,          // Foreign key to User
        Status = RequestStatus.Pending,
        CreatedAt = DateTime.Now
    };
    
    db.Requests.Add(request);
    await db.SaveChangesAsync();
    
    // 3. Create request items linked to this request
    foreach (var item in dto.items)
    {
        var requestItem = new RequestItem
        {
            RequestId = request.Id,  // Foreign key to Request
            ItemId = item.itemId,    // Foreign key to Item
            QuantityRequested = item.quantityRequested,
            Status = RequestItemStatus.PendingWithIssuer
        };
        
        db.RequestItems.Add(requestItem);
    }
    
    await db.SaveChangesAsync();
    return request;
}
```

### **Step 3: Database stores data**

**Request Table (NEW ROW):**
```
Id | UserId | Status  | CreatedAt
---|--------|---------|----------
42 | 1      | Pending | 2026-06-17
```

**RequestItem Table (NEW ROWS):**
```
Id | RequestId | ItemId | QuantityRequested | Status
---|-----------|--------|-------------------|------------------
100| 42        | 5      | 10                | PendingWithIssuer
101| 42        | 8      | 5                 | PendingWithIssuer
```

### **Step 4: Frontend receives response**
```json
{
  "id": 42,
  "userId": 1,
  "status": "Pending",
  "items": [
    { "itemId": 5, "quantityRequested": 10 },
    { "itemId": 8, "quantityRequested": 5 }
  ]
}
```

---

## Complete Database Diagram

```
┌─────────────────┐
│   Department    │
├─────────────────┤
│ Id (PK)         │
│ Name            │
└────────┬────────┘
         │
         │ One Department has Many Users
         │
         │ (DepartmentId is Foreign Key)
         │
         ▼
┌─────────────────┐
│      User       │
├─────────────────┤
│ Id (PK)         │
│ Username        │
│ Email           │
│ PasswordHash    │
│ DepartmentId(FK)│◄── Links to Department.Id
│ Role            │
│ IsActive        │
│ IsApproved      │
│ CreatedAt       │
└────────┬────────┘
         │
         │ One User has Many Requests
         │
         │ (UserId is Foreign Key)
         │
         ▼
┌─────────────────┐          ┌─────────────────┐
│    Request      │          │    Category     │
├─────────────────┤          ├─────────────────┤
│ Id (PK)         │          │ Id (PK)         │
│ UserId (FK)  ───┼──┐       │ Name            │
│ CategoryId(FK)──┼──┼────►  └─────────────────┘
│ Status          │  │
│ CreatedAt       │  │
└────────┬────────┘  │
         │           │
         │ One Request has Many RequestItems
         │
         ▼
┌─────────────────┐
│   RequestItem   │
├─────────────────┤
│ Id (PK)         │
│ RequestId (FK)  │◄── Links to Request.Id
│ ItemId (FK)     │◄── Links to Item.Id
│ QtyRequested    │
│ QtyApproved     │
│ QtyIssued       │
│ Status          │
└─────────────────┘
```

---

## Types of Relationships

### **1. One-to-Many (1:N)**
One Department has Many Users.
```
Department (1) ──► User (Many)
```
**In Code:**
```csharp
public class Department
{
    public int Id { get; set; }
    public ICollection<User> Users { get; set; }  // Many users
}

public class User
{
    public int DepartmentId { get; set; }  // Belongs to one department
}
```

### **2. Many-to-One (N:1)**
Many Users belong to One Department.
```
User (Many) ──► Department (1)
```

### **3. One-to-Many (1:N) - Another Example**
One Request has Many RequestItems.
```
Request (1) ──► RequestItem (Many)
```

---

## How Entity Framework Translates C# to SQL

### **C# Code:**
```csharp
var user = await db.Users
    .Include(u => u.Department)
    .FirstOrDefaultAsync(u => u.Email == "admin@gmail.com");
```

### **Translates to SQL:**
```sql
SELECT u."Id", u."Username", u."Email", u."DepartmentId", 
       d."Id", d."Name"
FROM "User" u
LEFT JOIN "Department" d ON u."DepartmentId" = d."Id"
WHERE u."Email" = 'admin@gmail.com'
LIMIT 1;
```

### **Result in C#:**
```csharp
User {
    Id: 1,
    Username: "System Admin",
    Email: "admin@gmail.com",
    DepartmentId: 1,
    Department: {
        Id: 1,
        Name: "Admin"
    }
}
```

---

## Backend Architecture: Files & Layers

```
backend/
├── Program.cs                 ← App startup & configuration
├── Controllers/               ← Receive HTTP requests
│   ├── AuthController.cs      ← Handles login/logout
│   ├── RequestController.cs   ← Handles requests
│   └── ...
├── Services/                  ← Business logic
│   ├── AuthService.cs         ← Login, JWT generation
│   ├── RequestService.cs      ← Request creation, approval
│   └── ...
├── Repositories/              ← Database queries
│   ├── UserRepository.cs      ← User queries
│   ├── RequestRepository.cs   ← Request queries
│   └── ...
├── Models/                    ← C# classes (map to tables)
│   ├── User.cs
│   ├── Department.cs
│   ├── Request.cs
│   ├── RequestItem.cs
│   └── ...
├── Data/
│   └── AppDbContext.cs        ← Database configuration
└── appsettings.json           ← Connection string & config
```

---

## Data Flow: Request → Database → Response

```
1. USER MAKES REQUEST
   Frontend: POST /api/auth/login
   { "email": "admin@gmail.com", "password": "admin@123" }

2. CONTROLLER RECEIVES
   AuthController.Login(LoginDto dto)
   ↓

3. SERVICE PROCESSES
   AuthService.LoginAsync(email, password)
   - Find user in database
   - Verify password
   - Generate JWT
   ↓

4. REPOSITORY QUERIES DATABASE
   UserRepository.GetByEmailAsync(email)
   SELECT * FROM "User" WHERE "Email" = ?
   ↓

5. DATABASE RETURNS DATA
   User record with encrypted password

6. SERVICE VALIDATES
   BCrypt.Verify(plainPassword, storedHash)
   ↓

7. RESPONSE GENERATED
   { "token": "eyJhbGc...", "message": "Login successful" }

8. FRONTEND RECEIVES
   Stores token in localStorage
   Uses in future requests
```

---

## Key Concepts Summary

| Concept | Meaning |
|---------|---------|
| **Primary Key (PK)** | Unique identifier (Id) - no two rows can have same Id |
| **Foreign Key (FK)** | Column in one table pointing to Primary Key in another table |
| **Relationship** | Connection between tables through foreign keys |
| **One-to-Many** | One record in table A linked to many records in table B |
| **JOIN** | Combine data from multiple tables for querying |
| **Entity Framework** | ORM that converts C# code to SQL automatically |
| **DTO** | Data Transfer Object - what API sends/receives |
| **Model** | C# class that represents a database table |

---

## Common Queries

### **Get user with department**
```csharp
var user = await db.Users
    .Include(u => u.Department)
    .FirstOrDefaultAsync(u => u.Id == 1);
```

### **Get all users in IT department**
```csharp
var itUsers = await db.Users
    .Where(u => u.Department.Name == "IT")
    .ToListAsync();
```

### **Get request with all items and user**
```csharp
var request = await db.Requests
    .Include(r => r.User)
    .Include(r => r.RequestItems)
    .FirstOrDefaultAsync(r => r.Id == 42);
```

### **Get requests created by a user**
```csharp
var userRequests = await db.Requests
    .Where(r => r.UserId == 1)
    .OrderByDescending(r => r.CreatedAt)
    .ToListAsync();
```

---

## Your App's Flow

1. **User logs in** → Email + Password sent to backend
2. **Backend verifies** → Checks User table, verifies password hash
3. **JWT generated** → Includes user ID, email, role
4. **Token returned** → Frontend stores it
5. **User creates request** → Frontend sends request with token
6. **Backend validates** → Checks token is valid
7. **Request stored** → New row in Request table + RequestItems table
8. **Response sent** → Request ID returned to frontend
9. **Frontend updates** → Shows user's requests list
10. **Data linked** → User → Request → RequestItems all connected by foreign keys

---

This is how your entire app works! The database is just organized tables, and foreign keys are like "pointers" linking them together.
