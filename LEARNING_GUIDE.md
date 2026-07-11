# 📚 Learning Guide: Backend & Database Architecture

I've created comprehensive documentation to help you understand your backend. Here's where to start:

---

## 📖 Documentation Files

### **1. QUICK_BACKEND_EXPLANATION.md** ← START HERE
- **Best for**: Quick overview, simple analogies
- **Time**: 10 minutes
- **Contains**: 
  - Database as filing cabinet analogy
  - Foreign keys as pointers
  - Complete example: Creating a request
  - 3-layer backend architecture
  - Common questions answered

### **2. BACKEND_DATABASE_EXPLAINED.md** ← DETAILED REFERENCE
- **Best for**: Understanding complete system
- **Time**: 30 minutes
- **Contains**:
  - 3-layer architecture explanation
  - Complete database diagram
  - All table structures with examples
  - How Entity Framework works
  - Real-world example: Creating a request step-by-step
  - SQL vs C# code translations
  - File organization & layers

### **3. DATABASE_RELATIONSHIPS_VISUAL.md** ← VISUAL LEARNER
- **Best for**: Understanding relationships with diagrams
- **Time**: 20 minutes  
- **Contains**:
  - Visual diagrams of all relationships
  - Data flow: Creating a request
  - All tables connected diagram
  - Cascade delete behavior
  - SQL query examples
  - ASCII art diagrams

---

## 🎯 Learning Path

### **If you want quick understanding (15 min)**
1. Read: QUICK_BACKEND_EXPLANATION.md
2. Focus on: "Complete Example: When Someone Creates a Request"
3. Done! You understand the basics

### **If you want full understanding (1 hour)**
1. Start with: QUICK_BACKEND_EXPLANATION.md (10 min)
2. Then: BACKEND_DATABASE_EXPLAINED.md (30 min)
3. Then: DATABASE_RELATIONSHIPS_VISUAL.md (20 min)
4. Reference: SQL vs C# code translations

### **If you're a visual learner (30 min)**
1. Start with: DATABASE_RELATIONSHIPS_VISUAL.md
2. Focus on diagrams and visual flow
3. Refer to: QUICK_BACKEND_EXPLANATION.md for explanations

### **If you want to dive deep (2 hours)**
1. Read all three files thoroughly
2. Look at actual code in backend/Models/
3. Look at actual code in backend/Services/
4. Try writing your own queries

---

## 🔑 Key Concepts You'll Learn

### **Database Concepts**
- [ ] What are tables and rows
- [ ] What is a Primary Key (PK)
- [ ] What is a Foreign Key (FK)
- [ ] One-to-Many relationships
- [ ] How JOIN works

### **Architecture Concepts**
- [ ] Controller-Service-Repository pattern
- [ ] How HTTP requests are processed
- [ ] How data flows from Frontend → Backend → Database
- [ ] What Entity Framework does

### **Your Specific App**
- [ ] How User table links to Department
- [ ] How Request links to User
- [ ] How RequestItem links to Request
- [ ] How category organizes requests
- [ ] Complete data flow from login to request creation

---

## 💡 Quick Reference

### **Your Database Tables**
```
User (people using app)
│
├─ DepartmentId (Foreign Key → Department)
│
└─ Creates → Request
             │
             ├─ UserId (Foreign Key → User)
             ├─ CategoryId (Foreign Key → Category)
             │
             └─ Has → RequestItem
                     │
                     ├─ RequestId (Foreign Key → Request)
                     └─ ItemId (Foreign Key → Item)

Department (teams)
│
└─ Has → User (many users per department)

Category (types of requests)
│
└─ Has → Request (many requests per category)
```

### **Backend 3 Layers**
```
1. Controller  ← Receives HTTP request from frontend
2. Service     ← Does business logic & database operations
3. Repository  ← Queries database & returns data
```

### **Data Flow Example**
```
Frontend: POST /api/auth/login
└─ Controller: AuthController.Login()
   └─ Service: AuthService.LoginAsync()
      └─ Repository: UserRepository.GetByEmailAsync()
         └─ Database: SELECT * FROM "User"
            └─ Returns user data
```

---

## 🔍 Things To Look At In Code

### **Models** (How C# represents database tables)
```
backend/Models/
├─ User.cs
├─ Department.cs
├─ Request.cs
├─ RequestItem.cs
└─ Category.cs
```

Each model has:
- Properties that match database columns
- Foreign keys (int Id, object reference)
- Collections for relationships (ICollection<>)

### **Services** (Business logic)
```
backend/Services/
├─ AuthService.cs      ← Login logic
├─ RequestService.cs   ← Request creation logic
└─ ...
```

Services:
- Call repositories to query database
- Process data
- Validate business rules
- Return results to controllers

### **Repositories** (Database queries)
```
backend/Repositories/
├─ UserRepository.cs      ← User queries
├─ RequestRepository.cs   ← Request queries
└─ ...
```

Repositories:
- Execute database queries
- Return raw data
- Don't have business logic

### **Controllers** (API endpoints)
```
backend/Controllers/
├─ AuthController.cs      ← /api/auth/* endpoints
├─ RequestController.cs   ← /api/requests/* endpoints
└─ ...
```

Controllers:
- Map HTTP routes to methods
- Call services
- Return responses to frontend

---

## 📝 Study Tips

1. **Read the quick explanation first** - Don't jump straight to details
2. **Look at diagrams** - Visual understanding is faster
3. **Find a simple example** - Creating a request is a good one
4. **Trace the flow** - Frontend → Controller → Service → Repository → DB
5. **Read actual code** - After understanding concepts, look at Models/ and Services/
6. **Write queries** - Try writing SQL to understand relationships
7. **Ask questions** - If something isn't clear, re-read that section

---

## 🎓 Terminology You'll Encounter

| Term | Simple Explanation |
|------|-------------------|
| **Table** | Spreadsheet with rows and columns |
| **Row** | One record (one user, one request, etc.) |
| **Column** | One field (email, name, id, etc.) |
| **Primary Key** | Unique ID for each row |
| **Foreign Key** | Link to another table's primary key |
| **JOIN** | Combine data from multiple tables |
| **ORM** | Tool that converts C# code to SQL |
| **Entity Framework** | Your app's ORM (Entity Framework Core) |
| **Repository** | Class that queries database |
| **Service** | Class with business logic |
| **Controller** | Class that handles HTTP requests |
| **DTO** | Data Transfer Object (what API sends/receives) |

---

## ❓ Common Questions Answered

**Q: Why do we need Foreign Keys?**
A: To link data between tables and prevent orphaned records.

**Q: What's the difference between Service and Repository?**
A: Service has business logic, Repository just queries database.

**Q: Why three layers (Controller → Service → Repository)?**
A: Separation of concerns - easier to test, maintain, and modify.

**Q: Can I delete a user?**
A: Only if they have no requests. Foreign keys prevent orphaned data.

**Q: Where is the password stored?**
A: As a BCrypt hash (encrypted), never as plain text.

**Q: What is JWT token?**
A: Temporary proof of login that frontend sends with each request.

**Q: Why does token expire after 24 hours?**
A: Security - if token is stolen, hacker only has access for 24 hours.

**Q: How do I query the database?**
A: Use LINQ in C# (looks like English) or write SQL directly.

---

## 🚀 Next Steps

1. **Understand the architecture** → Read QUICK_BACKEND_EXPLANATION.md
2. **Learn the details** → Read BACKEND_DATABASE_EXPLAINED.md  
3. **Study relationships** → Read DATABASE_RELATIONSHIPS_VISUAL.md
4. **Look at real code** → Check backend/Models/ and backend/Services/
5. **Write queries** → Try creating requests and queries yourself
6. **Debug & explore** → Use pgAdmin to see actual data in database
7. **Extend the app** → Add new features using same pattern

---

## 📚 Files to Study

### **C# Models** (Understand data structure)
- `backend/Models/User.cs`
- `backend/Models/Request.cs`
- `backend/Models/RequestItem.cs`
- `backend/Models/Department.cs`

### **Database Context** (How data is organized)
- `backend/Data/AppDbContext.cs` ← This defines all tables

### **Services** (Learn the pattern)
- `backend/Services/AuthService.cs` ← Login logic
- `backend/Services/RequestService.cs` ← Request logic

### **Controllers** (See the API)
- `backend/Controllers/AuthController.cs`
- `backend/Controllers/RequestController.cs`

---

## 🎯 Success Criteria

After reading these docs, you should understand:

- [ ] How database tables are connected by foreign keys
- [ ] What one-to-many relationships mean
- [ ] How a Request is linked to a User
- [ ] How RequestItems are linked to Request
- [ ] The 3-layer architecture (Controller → Service → Repository)
- [ ] How frontend HTTP requests reach the database
- [ ] How data flows back from database to frontend
- [ ] Why we use Entity Framework instead of writing raw SQL
- [ ] How passwords are hashed and verified
- [ ] How JWT tokens work and expire

If you understand these, you have solid foundation knowledge! 🎉

---

## 💬 Questions?

Look for answers in:
1. QUICK_BACKEND_EXPLANATION.md - Common Questions section
2. BACKEND_DATABASE_EXPLAINED.md - Troubleshooting section
3. DATABASE_RELATIONSHIPS_VISUAL.md - Key Terms section

---

**Happy Learning!** 📖
