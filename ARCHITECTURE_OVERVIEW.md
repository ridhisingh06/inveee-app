# Inventory Management System - Architecture Overview

## Project Summary
**InvMgmt** is a comprehensive inventory management system built with a modern, scalable architecture. It provides role-based access control, inventory tracking, personnel management, request workflows, and billing capabilities.

---

## System Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────┐
│                         CLIENT LAYER                                │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │  Angular 21.2 Web Application (Invmgmt-master)              │  │
│  │  - TypeScript / RxJS                                        │  │
│  │  - Responsive UI Components                                 │  │
│  │  - Role-based Navigation & Dashboards                       │  │
│  └──────────────────────────────────────────────────────────────┘  │
└──────────────────────┬──────────────────────────────────────────────┘
                       │ HTTP/REST + JWT Auth
                       ▼
┌──────────────────────────────────────────────────────────────────────┐
│                    API LAYER (invmgmt.web)                           │
│               ASP.NET Core 10.0 Web API                              │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │  Controllers (13 endpoints)                                  │  │
│  │  ├─ AuthController           (Login, Token Generation)      │  │
│  │  ├─ RegistrationController   (User Requests)               │  │
│  │  ├─ AdminController          (Admin Operations)            │  │
│  │  ├─ InventoryController      (Stock Management)            │  │
│  │  ├─ RequestController        (Request Management)          │  │
│  │  ├─ PersonnelController      (Personnel CRUD)             │  │
│  │  ├─ BillsController          (Billing & Challan)          │  │
│  │  ├─ IssuerController         (Issue Management)           │  │
│  │  ├─ ItemCategoryController   (Category Management)        │  │
│  │  └─ SectionWiseQueryController (Reports)                  │  │
│  └──────────────────────────────────────────────────────────────┘  │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │  Services Layer                                              │  │
│  │  ├─ IAuthService             ├─ IPersonnelService         │  │
│  │  ├─ IRegistrationService     ├─ ISectionWiseQueryService  │  │
│  │  ├─ IRequestService          ├─ IBillService             │  │
│  │  └─ Identity Management      └─ JWT Token Handling        │  │
│  └──────────────────────────────────────────────────────────────┘  │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │  Repository Pattern (Data Access)                            │  │
│  │  ├─ IUserRepository          ├─ IPersonnelRepository      │  │
│  │  ├─ IRegistrationRepository  ├─ IRequestRepository        │  │
│  │  └─ Generic CRUD Operations  └─ Abstraction Layer         │  │
│  └──────────────────────────────────────────────────────────────┘  │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │  Middleware & Utilities                                      │  │
│  │  ├─ Global Exception Handler                               │  │
│  │  ├─ JWT Bearer Authentication                              │  │
│  │  ├─ TraceIdEnricherMiddleware (Request Logging)            │  │
│  │  ├─ Serilog Integration (Structured Logging)               │  │
│  │  ├─ PasswordUtils (BCrypt Hashing)                         │  │
│  │  └─ ClaimsPrincipalExtensions (Authorization)              │  │
│  └──────────────────────────────────────────────────────────────┘  │
└──────────────────────┬──────────────────────────────────────────────┘
                       │ Entity Framework Core
                       ▼
┌──────────────────────────────────────────────────────────────────────┐
│                      DATA LAYER                                       │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │  Database Context (AppDbContext)                             │  │
│  │  - 19 Entity Models mapped to PostgreSQL                     │  │
│  │  - EF Migrations for Schema Management                       │  │
│  │  - Connection Pooling & Resilience                           │  │
│  └──────────────────────────────────────────────────────────────┘  │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │  PostgreSQL Database (v15)                                    │  │
│  │  - Normalized Relational Schema                              │  │
│  │  - Indexed for Query Performance                             │  │
│  │  - Transactional Integrity                                   │  │
│  └──────────────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────────────┐
│                    SUPPORTING SERVICES                                │
│  ┌──────────────────┐  ┌──────────────────┐  ┌─────────────────┐   │
│  │  Seq (Logging)   │  │  File Storage    │  │  Health Checks  │   │
│  │  (Port 8082)     │  │  (wwwroot/)      │  │  (/health)      │   │
│  └──────────────────┘  └──────────────────┘  └─────────────────┘   │
└──────────────────────────────────────────────────────────────────────┘
```

---

## Technology Stack

### Backend
| Layer | Technology | Version |
|-------|-----------|---------|
| Framework | ASP.NET Core | 10.0.5 |
| Language | C# | .NET 10.0 |
| ORM | Entity Framework Core | 10.0.5 |
| Database | PostgreSQL | 15 |
| Authentication | JWT Bearer | Microsoft.AspNetCore.Authentication.JwtBearer |
| Logging | Serilog | 9.0.0 |
| Password Hashing | BCrypt.Net-Next | 4.2.0 |
| API Documentation | Swagger/OpenAPI | 10.1.7 |

### Frontend
| Layer | Technology | Version |
|-------|-----------|---------|
| Framework | Angular | 21.2.8 |
| Language | TypeScript | 5.9.2 |
| Reactive Programming | RxJS | 7.8.0 |
| Testing | Vitest | 4.0.8 |
| Build Tool | Angular CLI | 21.2.6 |
| Package Manager | npm | 11.2.0 |

### DevOps & Infrastructure
| Component | Technology |
|-----------|-----------|
| Containerization | Docker |
| Orchestration | Docker Compose |
| Web Server (Frontend) | Nginx |
| Logging Backend | Seq (Structured Logs) |
| Database | PostgreSQL |

---

## Core Entity Models

### User Management
```
User
├── Id (PK)
├── Username
├── Email
├── PasswordHash (BCrypt)
├── DepartmentId (FK → Department)
├── Designation
├── Role (ADMIN, ISSUER, USER)
├── IsActive
├── IsApproved
├── CreatedAt
└── Relationships: UserRole, Request, ApprovalLog
```

### Role & Authorization
```
Role                          UserRole
├── Id (PK)                   ├── UserId (FK)
├── Name                      ├── RoleId (FK)
│   (User, Issuer, Admin)     └── AssignedDate
└── RoleItemLimit (many-to-many)
```

### Inventory Management
```
Category                    Item                        InventoryStock
├── Id (PK)                 ├── Id (PK)                 ├── Id (PK)
├── Name                    ├── Name (Unique)           ├── ItemId (FK)
└── Items (1:N)             ├── CategoryId (FK)         ├── Quantity
                            ├── Description             ├── ReorderLevel
                            ├── UnitPrice               ├── UpdatedAt
                            ├── CreatedAt               └── Item (1:1)
                            ├── InventoryStock (1:1)
                            └── RequestItems (1:N)

RoleItemLimit
├── RoleId (FK)
├── ItemId (FK)
└── MaxQuantity
```

### Request Workflow
```
Request                           RequestItem
├── Id (PK)                       ├── Id (PK)
├── UserId (FK → User)            ├── RequestId (FK)
├── Status (Pending, Approved,    ├── ItemId (FK)
│          Issued, Received)       ├── QuantityRequested
├── CreatedAt                      ├── QuantityApproved
├── UpdatedAt                      ├── QuantityIssued
├── RequestItems (1:N)            ├── Status (Pending, Approved,
└── ApprovalLogs (1:N)            │          Issued, Received)
                                  └── UpdatedAt
```

### Approval & Audit
```
ApprovalLog                   AuditLog
├── Id (PK)                   ├── Id (PK)
├── RequestId (FK)            ├── Entity
├── ApprovedBy (User)         ├── Action
├── Status                    ├── Changes
├── Comments                  ├── Timestamp
└── ApprovedAt                └── User

IssueLog                      ReceivedLog
├── Id (PK)                   ├── Id (PK)
├── RequestItemId (FK)        ├── RequestItemId (FK)
├── IssuedQuantity            ├── ReceivedQuantity
├── IssuedAt                  └── ReceivedAt
└── IssuedBy
```

### Personnel Management
```
Personnel
├── Id (PK)
├── Name
├── Email (Unique)
├── Phone
├── Designation
├── Department
├── PhotoUrl
├── DateOfBirth
├── Address
├── CreatedAt
└── UpdatedAt
```

### Registration & Bills
```
RegistrationRequest           Bill
├── Id (PK)                   ├── Id (PK)
├── Username                  ├── BillNo (Unique)
├── Email                     ├── CreatedByUserId (FK)
├── Department                ├── BillDate
├── Designation               ├── TotalAmount
├── Status                    ├── CreatedAt
├── ApprovedBy                ├── BillItems (1:N)
└── CreatedAt                 └── CreatedByUser (1:N)

BillItem
├── Id (PK)
├── BillId (FK)
├── ItemId (FK)
├── Quantity
├── UnitPrice
└── Item (FK)
```

---

## API Endpoints Overview

### Authentication
- `POST /api/auth/login` - User login & JWT generation
- `POST /api/auth/logout` - Session termination
- `POST /api/auth/refresh-token` - Token refresh

### User Management
- `GET /api/admin/users` - List all users (Admin)
- `POST /api/registration/register` - Submit registration request
- `GET /api/admin/registrations` - View pending registrations
- `PUT /api/admin/approve-registration/{id}` - Approve registration

### Inventory Management
- `GET /api/inventory/items` - List items with stock
- `POST /api/inventory/items` - Add new item (Admin)
- `PUT /api/inventory/items/{id}` - Update item
- `GET /api/inventory/categories` - List categories
- `POST /api/inventory/categories` - Create category

### Request Workflow
- `POST /api/requests` - Create new request
- `GET /api/requests` - List user requests
- `GET /api/requests/{id}` - Request details
- `PUT /api/requests/{id}/approve` - Approve request (Admin/Issuer)
- `PUT /api/requests/{id}/issue` - Issue items (Issuer)
- `PUT /api/requests/{id}/receive` - Mark as received (User)

### Personnel Management
- `GET /api/personnel` - List personnel
- `POST /api/personnel` - Add personnel
- `PUT /api/personnel/{id}` - Update personnel
- `DELETE /api/personnel/{id}` - Remove personnel

### Bills & Challan
- `GET /api/bills` - List bills
- `POST /api/bills` - Create bill (Issuer)
- `GET /api/bills/{id}` - Bill details

### Reporting
- `GET /api/section-wise-query` - Generate reports by section

---

## Frontend Architecture

### Module Organization

```
src/app/
├── auth/                          # Authentication Module
│   ├── login/                     # Login Component
│   ├── register/                  # Registration Component
│   ├── Guard/                     # Route Guards & Interceptors
│   └── services/                  # AuthService
│
├── admin-dashboard/               # Admin Dashboard
├── admin-layout/                  # Admin Layout
├── admin-pending/                 # Pending Approvals
│
├── user-dashboard/                # User Dashboard
├── user-item-list/                # Item Browsing
├── user-cart/                     # Shopping Cart
├── user-check-status/             # Request Status
├── my-requests/                   # My Requests History
│
├── issuer-dashboard/              # Issuer Dashboard
├── issuer-issue/                  # Issue Items
├── issuer-approved/               # Approved Requests
│
├── inventory/                     # Inventory Management
├── item-category/                 # Category Management
├── category-management/
│
├── personnel-management/          # Personnel CRUD
├── personnel-details-new-entry/   # Add Personnel
│
├── request-item/                  # Request Item Module
├── monthly-register/              # Monthly Register Reports
├── section-wise-query/            # Section-wise Reports
│
├── delivery-challan-bill-entry/   # Billing
├── stores-section-allocation/     # Allocation Management
│
├── services/                      # Shared Services
│   ├── auth.service.ts
│   ├── request.service.ts
│   ├── inventory.service.ts
│   ├── personnel.service.ts
│   ├── cart.service.ts
│   ├── request-state.service.ts
│   └── ...
│
├── models/                        # TypeScript Interfaces
│   ├── request.model.ts
│   ├── item.ts
│   ├── personnel.model.ts
│   └── ...
│
├── utils/                         # Utility Functions
│   └── status.util.ts
│
├── navbar/                        # Navigation Component
├── admin-sidebar/                 # Sidebar Component
│
├── app.routes.ts                  # Routing Configuration
├── app.config.ts                  # App Configuration
└── main.ts                        # Bootstrap
```

### Component Hierarchy

```
AppComponent
├── Navbar
├── Router Outlet
│   ├── Auth Routes
│   │   ├── LoginComponent
│   │   └── RegisterComponent
│   │
│   ├── Admin Routes (Protected)
│   │   ├── AdminLayoutComponent
│   │   │   ├── AdminSidebar
│   │   │   ├── AdminDashboard
│   │   │   ├── AdminPending
│   │   │   └── CategoryManagement
│   │   └── ...
│   │
│   ├── User Routes (Protected)
│   │   ├── UserDashboard
│   │   ├── UserItemList
│   │   ├── UserCart
│   │   ├── MyRequests
│   │   └── ...
│   │
│   └── Issuer Routes (Protected)
│       ├── IssuerDashboard
│       ├── IssuerIssue
│       └── ...
└── Footer (optional)
```

### Service Communication Pattern

```
Components
    ↓
├─ RequestService (GET/POST /api/requests)
├─ InventoryService (GET /api/inventory/items)
├─ PersonnelService (GET/POST /api/personnel)
├─ AuthService (POST /api/auth/login)
├─ CartService (Local state management)
└─ RequestStateService (Shared state)
    ↓
HTTP Client
    ↓
API Gateway (Port 5001)
    ↓
Backend API
```

---

## Authentication & Authorization

### JWT Token Flow
```
1. User Credentials
   ↓ POST /api/auth/login
2. Backend Validates
   ├─ Check User exists
   ├─ Verify Password (BCrypt)
   └─ Check IsApproved flag
   ↓
3. JWT Token Generated
   ├─ Header: {alg: HS256, typ: JWT}
   ├─ Payload: {sub, email, role, exp, iat}
   └─ Signature: HMACSHA256(secret)
   ↓
4. Token Stored in Frontend
   └─ localStorage / sessionStorage
   ↓
5. All Requests Include Token
   └─ Authorization: Bearer <token>
   ↓
6. Backend Validates Token
   ├─ Verify Signature
   ├─ Check Expiration
   └─ Extract Claims
```

### Role-Based Access Control (RBAC)
```
Roles:
├─ USER
│  ├─ Can browse inventory
│  ├─ Can create requests
│  ├─ Can view own requests
│  └─ Can mark requests as received
│
├─ ISSUER
│  ├─ All USER permissions
│  ├─ Can view pending approvals
│  ├─ Can approve requests
│  ├─ Can issue items
│  └─ Can create bills
│
└─ ADMIN
   ├─ All ISSUER permissions
   ├─ Can manage users
   ├─ Can manage categories
   ├─ Can manage personnel
   ├─ Can set item limits per role
   └─ Can view audit logs
```

---

## Database Schema Relationships

### Key Relationships

**One-to-Many:**
- User (1) → Request (N)
- Category (1) → Item (N)
- Item (1) → RequestItem (N)
- Request (1) → RequestItem (N)
- Request (1) → ApprovalLog (N)
- Bill (1) → BillItem (N)

**One-to-One:**
- Item (1) ↔ InventoryStock (1)
- User (1) ← UserRole → Role (1)

**Polymorphic:**
- AuditLog (tracks changes on any entity)

### Indexes for Performance
```sql
RequestItem:
  - Index on (ItemId)
  - Index on (RequestId)
  - Index on (Status)

RegistrationRequest:
  - Index on (Status)

Personnel:
  - Unique Index on (Email)

Bill:
  - Unique Index on (BillNo)
  - Index on (CreatedAt)

BillItem:
  - Index on (BillId)
```

---

## Deployment Architecture

### Docker Compose Setup
```
┌─────────────────────────────────────────────┐
│         Docker Compose Network              │
├─────────────────────────────────────────────┤
│                                             │
│  Frontend Container (nginx:80)              │
│  ├─ Port: 4200 (external)                  │
│  ├─ Angular Build (dist/)                  │
│  └─ Nginx config (reverse proxy)           │
│      ↓                                      │
│  Backend Container (ASP.NET:5000)          │
│  ├─ Port: 5001 (external)                  │
│  ├─ API Endpoints                          │
│  ├─ Health Check (/health)                 │
│  ├─ Volume: /app/Logs                      │
│  ├─ Volume: /app/wwwroot/uploads           │
│  └─ Depends on: db, seq                    │
│      ↓                                      │
│  PostgreSQL (5432 internal)                │
│  ├─ Port: 5433 (external)                  │
│  ├─ Volume: pgdata                         │
│  ├─ Health Check: pg_isready               │
│  └─ Database: InvMgmtDb                    │
│      ↓                                      │
│  Seq Logging (Port 5342)                   │
│  ├─ Structured Logs UI (Port 8082)         │
│  ├─ Volume: seqdata                        │
│  └─ Health Check: HTTP GET                 │
│                                             │
│  Shared Volumes:                            │
│  ├─ pgdata (PostgreSQL data)               │
│  ├─ seqdata (Seq logs)                     │
│  └─ uploads (User files)                   │
│                                             │
└─────────────────────────────────────────────┘
```

### Environment Variables
```
Backend (.env):
  - ASPNETCORE_ENVIRONMENT
  - ASPNETCORE_HTTP_PORTS
  - ConnectionStrings__DefaultConnection
  - ADMIN_EMAIL
  - ADMIN_PASSWORD
  - JWT_KEY
  - Jwt__Issuer
  - Jwt__Audience
  - POSTGRES_USER
  - POSTGRES_PASSWORD

Frontend (.env):
  - API_BASE_URL
  - Environment (production/development)
```

---

## Data Flow Examples

### User Registration Flow
```
1. User fills registration form
   ↓
2. Angular validates input
   ↓
3. POST /api/registration/register
   ├─ Backend: Create RegistrationRequest
   ├─ Status: Pending
   └─ Notify Admin
   ↓
4. Admin reviews in Admin Dashboard
   ↓
5. Admin approves/rejects
   ├─ Create User in Users table
   ├─ Update RegistrationRequest.Status
   ├─ Generate JWT
   └─ Notify User
   ↓
6. User receives email with credentials
   ↓
7. User logs in (JWT flow begins)
```

### Item Request Flow
```
1. User browses inventory (GET /api/inventory/items)
   ↓
2. User adds items to cart (CartService)
   ↓
3. User submits request
   ├─ POST /api/requests
   ├─ Create Request (Status: Pending)
   ├─ Create RequestItems (one per item)
   └─ Notify Issuer
   ↓
4. Issuer reviews pending requests
   ├─ GET /api/requests (filter by Status=Pending)
   └─ Views item quantities & limits
   ↓
5. Issuer approves request
   ├─ PUT /api/requests/{id}/approve
   ├─ Update Request.Status: Approved
   ├─ Update RequestItem.Status: Approved
   ├─ Update RequestItem.QuantityApproved
   └─ Notify User
   ↓
6. Issuer issues items
   ├─ PUT /api/requests/{id}/issue
   ├─ Decrease InventoryStock
   ├─ Create IssueLog
   ├─ Update RequestItem.QuantityIssued
   └─ Notify User
   ↓
7. User receives items
   ├─ PUT /api/requests/{id}/receive
   ├─ Create ReceivedLog
   ├─ Update RequestItem.Status: Received
   ├─ Create AuditLog
   └─ Email confirmation
```

### Bill Generation Flow
```
1. Issuer compiles items for bill (Delivery Challan)
   ↓
2. POST /api/bills
   ├─ Create Bill (BillNo, CreatedAt)
   ├─ Create BillItems (link to Items)
   └─ Calculate TotalAmount
   ↓
3. Bill stored in database
   ├─ BillNo: Unique
   ├─ CreatedAt: Indexed
   └─ CreatedByUserId: Tracked
   ↓
4. Finance reviews bills
   ├─ GET /api/bills
   ├─ Filter by date range
   └─ Export for accounting
   ↓
5. Audit trail maintained
   ├─ AuditLog.Entity: "Bill"
   ├─ AuditLog.Action: "Created"
   └─ AuditLog.Changes: JSON diff
```

---

## Security Features

### Authentication
- ✅ JWT Bearer Token Authentication
- ✅ BCrypt Password Hashing (salt rounds)
- ✅ Token Expiration & Refresh
- ✅ Secure password transmission over HTTPS

### Authorization
- ✅ Role-Based Access Control (RBAC)
- ✅ Route Guards (Angular)
- ✅ Attribute-based authorization ([Authorize])
- ✅ Claim-based policies

### Data Protection
- ✅ SQL Injection Prevention (Parameterized Queries via EF)
- ✅ CORS Policy (Configurable origins)
- ✅ Global exception handling (no stack traces in production)
- ✅ Sensitive data logging disabled in production

### Audit & Compliance
- ✅ AuditLog tracking (Entity, Action, Changes, User)
- ✅ Structured logging with Serilog
- ✅ Request tracing (TraceId in all responses)
- ✅ Health checks (/health endpoint)

---

## Error Handling & Logging

### Logging Architecture
```
Application Code
       ↓
Serilog (ILogger)
├─ Console Sink
├─ File Sink (/Logs/log-*.txt)
└─ Seq Sink (http://seq:5341)
       ↓
Seq Dashboard (Port 8082)
├─ Structured Queries
├─ Real-time streaming
└─ Historical analysis
```

### Error Handling Pipeline
```
Global Exception Handler Middleware
       ↓
Catches all unhandled exceptions
       ↓
Logs with ILogger<Program>
├─ Exception type
├─ Stack trace
├─ Request path
└─ TraceId
       ↓
Response to Client (HTTP 500)
{
  "message": "An internal server error occurred.",
  "traceId": "unique-id",
  "timestamp": "2026-06-05T...",
  // In dev environment only:
  "exception": "ExceptionType",
  "stackTrace": "...",
  "path": "/api/..."
}
```

---

## Performance Optimizations

### Database
- ✅ Connection pooling (5 retries, 30s timeout)
- ✅ Indexed queries on frequently filtered columns
- ✅ Eager loading with `.Include()` to prevent N+1 queries
- ✅ Memory cache for lightweight data
- ✅ Command timeout: 30 seconds

### Backend
- ✅ Async/await for non-blocking I/O
- ✅ Dependency injection for efficient resource management
- ✅ Middleware optimization (ordering critical)
- ✅ Response compression (gzip)

### Frontend
- ✅ Lazy loading of modules
- ✅ RxJS operators (debounce, throttle) for API calls
- ✅ Change detection strategy optimization
- ✅ OnPush change detection for components

---

## Development Workflow

### Local Setup
```bash
# Backend
cd invmgmt.web
dotnet restore
dotnet ef database update
dotnet run

# Frontend
cd Invmgmt-master
npm install
ng serve

# Access
Frontend: http://localhost:4200
Backend API: http://localhost:5000
Swagger: http://localhost:5000/swagger
```

### Docker Setup
```bash
# Build and start all services
docker-compose up --build

# Access
Frontend: http://localhost:4200
Backend: http://localhost:5001
Seq: http://localhost:8082
PostgreSQL: localhost:5433
```

---

## Key Files & Configurations

### Backend Configuration Files
| File | Purpose |
|------|---------|
| `Program.cs` | Application startup, DI setup, middleware pipeline |
| `appsettings.json` | Default configuration |
| `appsettings.Development.json` | Dev environment overrides |
| `appsettings.Production.json` | Production secrets (gitignored) |
| `Dockerfile` | Container image definition |
| `invmgmt.web.csproj` | NuGet dependencies |

### Frontend Configuration Files
| File | Purpose |
|------|---------|
| `main.ts` | Application bootstrap |
| `app.config.ts` | App-level configuration |
| `app.routes.ts` | Routing configuration |
| `tsconfig.json` | TypeScript configuration |
| `angular.json` | Angular CLI configuration |
| `proxy.conf.json` | Dev proxy configuration |

### Database Files
| File | Purpose |
|------|---------|
| `Migrations/` | EF Core migration history |
| `AppDbContext.cs` | Entity configuration & relationships |
| `AppDbContextFactory.cs` | Factory for CLI tools |

---

## Deployment Checklist

- [ ] Update environment variables (JWT_KEY, DB password)
- [ ] Build frontend: `ng build --configuration production`
- [ ] Build backend: `dotnet publish -c Release`
- [ ] Update Docker image versions
- [ ] Configure PostgreSQL backup strategy
- [ ] Set up SSL/TLS certificates
- [ ] Configure CORS for production domain
- [ ] Review Serilog sink configuration
- [ ] Test health check endpoints
- [ ] Perform smoke tests on staging
- [ ] Document API endpoints for consumers
- [ ] Set up monitoring & alerts

---

## Support & Documentation

- API Documentation: `/swagger` (Swagger UI)
- System Health: `/health` (JSON endpoint)
- Logs: `/app/Logs/` (file storage) & Seq (8082)
- Database: Use `AppDbContext` for queries
- Authentication: JWT in Authorization header
- Error Codes: See global exception handler

---

## Conclusion

The InvMgmt system is built with a scalable, enterprise-grade architecture. It separates concerns across multiple layers, uses industry-standard patterns (Repository, DI, JWT), and provides comprehensive logging and error handling. The system is containerized for easy deployment and supports high availability through health checks and connection resilience.
