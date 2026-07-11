# Quick Backend & Database Explanation

## Simple Analogy

Think of your database like a **filing cabinet with organized drawers**:

```
┌─────────────────────────────────────┐
│     Database = Filing Cabinet       │
├─────────────────────────────────────┤
│  Drawer 1: User (People)            │
│  Drawer 2: Department (Teams)       │
│  Drawer 3: Request (Forms)          │
│  Drawer 4: RequestItem (Items)      │
│  Drawer 5: Category (Types)         │
└─────────────────────────────────────┘
```

Each drawer has **rows** (records) and **columns** (fields):

### **Users Drawer**
```
┌────┬──────────┬─────────────────┬───────────────┐
| Id | Name     | Email           | DepartmentId  |
├────┼──────────┼─────────────────┼───────────────┤
| 1  | Admin    | admin@gmail.com | 1 (points to Admin dept) |
| 2  | John     | john@gmail.com  | 2 (points to IT dept)   |
| 3  | Sarah    | sarah@gmail.com | 3 (points to HR dept)   |
└────┴──────────┴─────────────────┴───────────────┘
```

### **Department Drawer**
```
┌────┬─────────┐
| Id | Name    |
├────┼─────────┤
| 1  | Admin   |
| 2  | IT      |
| 3  | HR      |
| 4  | Finance |
└────┴─────────┘
```

---

## Foreign Keys = Pointers

**Foreign Key** is like a **sticky note** pointing from one drawer to another.

```
User's "DepartmentId" column = Pointer to Department table

User Admin (Id=1) has DepartmentId=1
                  │
                  └──► Points to Department (Id=1) = "Admin"

This means: Admin User works in Admin Department
```

---

## Complete Example: When Someone Creates a Request

### **Frontend sends:**
```json
{
  "items": [
    { "itemId": 5, "quantity": 10 }
  ]
}
```

### **Backend processes:**

**Step 1: Get the user from User table**
```sql
SELECT * FROM "User" WHERE "Email" = 'admin@gmail.com';
Result: User ID = 1
```

**Step 2: Create request pointing to User ID 1**
```sql
INSERT INTO "Request" ("UserId", "Status", "CreatedAt")
VALUES (1, 'Pending', NOW());
Result: Request ID = 42
```

**Step 3: Create request item pointing to Request ID 42**
```sql
INSERT INTO "RequestItem" ("RequestId", "ItemId", "QuantityRequested")
VALUES (42, 5, 10);
Result: RequestItem ID = 100
```

### **Now the database has:**

```
Request Table:
┌────┬────────┬─────────┐
| Id | UserId | Status  |
├────┼────────┼─────────┤
| 42 | 1      | Pending | ← User 1's request
└────┴────────┴─────────┘

RequestItem Table:
┌────┬───────────┬────────┬──────────────────┐
| Id | RequestId | ItemId | QuantityRequested|
├────┼───────────┼────────┼──────────────────┤
|100 | 42        | 5      | 10               | ← Belongs to Request 42
└────┴───────────┴────────┴──────────────────┘
```

**All connected by Foreign Keys:**
- RequestItem.RequestId (100) points to Request.Id (42)
- Request.UserId (42) points to User.Id (1)
- User.DepartmentId (1) points to Department.Id (1)

---

## How Backend Code Works

### **Backend has 3 layers:**

```
┌──────────────────┐
│   Controller     │  ← Receives HTTP request
│                  │  ← AuthController.Login()
└────────┬─────────┘
         │ calls
         ▼
┌──────────────────┐
│    Service       │  ← Does business logic
│                  │  ← AuthService.LoginAsync()
│  - Check password│  ← Verify BCrypt hash
│  - Generate JWT │  ← Create token
└────────┬─────────┘
         │ queries
         ▼
┌──────────────────┐
│  Repository      │  ← Talks to database
│                  │  ← UserRepository.GetByEmail()
│  Runs SQL        │  ← SELECT * FROM "User"...
└──────────────────┘
```

### **Example: Login Flow**

```csharp
// 1. Controller receives request
[HttpPost("login")]
public async Task<IActionResult> Login(LoginDto dto)
{
    // 2. Call service
    var result = await authService.LoginAsync(dto.Email, dto.Password);
    
    if (!result.Success)
        return Unauthorized(new { message = result.Message });
    
    // 3. Return token
    return Ok(new { token = result.Token, message = "Login successful." });
}

// Service (layer 2)
public async Task<AuthResult> LoginAsync(string email, string password)
{
    // 4. Call repository to get user
    var user = await userRepository.GetByEmailAsync(email);
    
    if (user == null)
        return new AuthResult { Success = false, Message = "User not found" };
    
    // 5. Verify password
    bool isValid = PasswordUtils.VerifyPassword(password, user.PasswordHash);
    
    if (!isValid)
        return new AuthResult { Success = false, Message = "Incorrect password" };
    
    // 6. Generate JWT
    var token = GenerateJwtToken(user);
    
    return new AuthResult { Success = true, Token = token, Message = "Login successful." };
}

// Repository (layer 3)
public async Task<User> GetByEmailAsync(string email)
{
    // 7. Query database
    return await db.Users
        .FirstOrDefaultAsync(u => u.Email == email);
}
```

---

## Backend to Database Translation

### **What you write (C#):**
```csharp
var user = await db.Users
    .Include(u => u.Department)
    .FirstOrDefaultAsync(u => u.Email == "admin@gmail.com");
```

### **What gets sent to database (SQL):**
```sql
SELECT u."Id", u."Username", u."Email", u."DepartmentId",
       d."Id", d."Name"
FROM "User" u
LEFT JOIN "Department" d ON u."DepartmentId" = d."Id"
WHERE u."Email" = 'admin@gmail.com'
LIMIT 1;
```

### **What you get back (C# object):**
```csharp
{
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

## Your Database Tables

```
┌────────────────────────────────────────────────────────┐
│                  Your Database                         │
├────────────────────────────────────────────────────────┤
│                                                        │
│  User                 Request               Item       │
│  ├─ Id (PK)          ├─ Id (PK)           ├─ Id (PK)  │
│  ├─ Username         ├─ UserId (FK)───┐   ├─ Name     │
│  ├─ Email            ├─ CategoryId(FK)│   └─ ...      │
│  ├─ PasswordHash     └─ Status        │                │
│  └─ DepartmentId─┐                    │                │
│     (FK)         │   RequestItem       │                │
│                  └──► ├─ Id (PK)       │                │
│  Department          ├─ RequestId(FK)─┼────────┐       │
│  ├─ Id (PK)         ├─ ItemId(FK)─────┼────────┼───┐  │
│  └─ Name            └─ Quantity       │        │   │  │
│                                        │        │   │  │
│  Category                              │        │   │  │
│  ├─ Id (PK)                           │        │   │  │
│  └─ Name ◄──────────────────────────────┘        │   │  │
│                                                   │   │  │
│                                                   └───┘  │
│                                                        │
└────────────────────────────────────────────────────────┘
```

---

## Key Takeaways

1. **Database = Organized tables** like spreadsheets
2. **Foreign Key = Pointer** between tables
3. **One-to-Many = One record linked to many records**
   - One User can create Many Requests
   - One Request can have Many RequestItems
4. **Backend translates C# code to SQL automatically** (Entity Framework)
5. **Controller → Service → Repository → Database** flow

---

## Data Types

```
┌──────────┬─────────────────────────┐
| Type     | Example                 |
├──────────┼─────────────────────────┤
| Int      | 1, 42, 100             |
| String   | "admin@gmail.com"       |
| Boolean  | true, false             |
| DateTime | 2026-06-17 12:30:45    |
└──────────┴─────────────────────────┘
```

---

## Common Questions

**Q: What if user doesn't have a department?**
A: DepartmentId is `nullable` (int?), so it can be null. Not every user must have a department.

**Q: What if I delete a user?**
A: Foreign key constraint prevents deletion if requests exist. You must delete requests first.

**Q: Can a request have multiple items?**
A: Yes! RequestItem is the link. One Request ID can have many RequestItem rows.

**Q: Where is password stored?**
A: As BCrypt hash in User.PasswordHash column. Never stored as plain text!

---

Read **BACKEND_DATABASE_EXPLAINED.md** for more detailed information.
