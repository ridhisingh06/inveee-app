# Key Files Reference Guide

Quick reference to important files and their purposes in the Inveee App project.

---

## 🔧 Configuration Files

| File | Location | Purpose | Type |
|------|----------|---------|------|
| `Program.cs` | `invmgmt.web/` | Application startup, middleware setup | C# |
| `appsettings.json` | `invmgmt.web/` | Production app settings | JSON |
| `appsettings.Development.json` | `invmgmt.web/` | Development settings | JSON |
| `angular.json` | `Invmgmt-master/` | Angular CLI configuration | JSON |
| `tsconfig.json` | `Invmgmt-master/` | TypeScript configuration | JSON |
| `package.json` | `Invmgmt-master/` | NPM dependencies & scripts | JSON |
| `invmgmt.web.csproj` | `invmgmt.web/` | C# project dependencies | XML |
| `docker-compose.yml` | Root | Local development environment | YAML |
| `.env.example` | Root | Environment variables template | Text |
| `.env.prod` | Root | Production env variables (ignored) | Text |
| `task-definition.json` | Root | ECS task definition | JSON |

---

## 🏗️ Infrastructure Files

| File | Location | Purpose | Type |
|------|----------|---------|------|
| `main.tf` | `terraform/` | AWS infrastructure definition | HCL |
| `variables.tf` | `terraform/` | Terraform variables & defaults | HCL |
| `outputs.tf` | `terraform/` | Terraform output values | HCL |
| `terraform.tfstate` | `terraform/` | Current infrastructure state | JSON |
| `.terraform.lock.hcl` | `terraform/` | Provider version lock | HCL |
| `Dockerfile` | `invmgmt.web/` | Backend Docker image | Dockerfile |
| `Dockerfile` | `Invmgmt-master/` | Frontend Docker image | Dockerfile |
| `nginx.default.conf` | `Invmgmt-master/` | Nginx configuration | Conf |
| `entrypoint.sh` | `Invmgmt-master/` | Docker container startup script | Shell |

---

## 🚀 CI/CD Files

| File | Location | Purpose | Type |
|------|----------|---------|------|
| `deploy.yml` | `.github/workflows/` | Main CI/CD pipeline | YAML |
| `terraform.yml` | `.github/workflows/` | Terraform deployment workflow | YAML |

---

## 📝 Database Files

| File | Location | Purpose | Type |
|------|----------|---------|------|
| `AppDbContext.cs` | `invmgmt.web/Data/` | Entity Framework context | C# |
| `Migrations/` | `invmgmt.web/` | EF Core migration files | C# |
| `AppDbContextModelSnapshot.cs` | `invmgmt.web/Migrations/` | Latest schema snapshot | C# |
| `query.sql` | `invmgmt.web/` | Query examples | SQL |
| `insert.sql` | `invmgmt.web/` | Insert examples | SQL |
| `seed_cat.sql` | `invmgmt.web/` | Seed categories | SQL |
| `personnel_migration.sql` | `invmgmt.web/` | Personnel data migration | SQL |

---

## 🛡️ Security Files

| File | Location | Purpose | Type |
|------|----------|---------|------|
| `.env.prod` | Root | Secrets (gitignored) | Text |
| `appsettings.Production.json` | `invmgmt.web/` | Prod config | JSON |
| (GitHub Secrets) | GitHub | AWS credentials | Configuration |

---

## 🧪 Test Files

| File | Location | Purpose | Type |
|------|----------|---------|------|
| `test_auth.ps1` | `invmgmt.web/` | Authentication tests | PowerShell |
| `test_reg.ps1` | `invmgmt.web/` | Registration tests | PowerShell |
| `test_dup.ps1` | `invmgmt.web/` | Duplicate tests | PowerShell |
| `test_getitems.ps1` | `invmgmt.web/` | Get items tests | PowerShell |
| `test_additem.ps1` | `invmgmt.web/` | Add item tests | PowerShell |
| `test_reg_null.ps1` | `invmgmt.web/` | Null registration tests | PowerShell |
| `run_cat.ps1` | `invmgmt.web/` | Category tests | PowerShell |

---

## 📚 Documentation Files

| File | Location | Purpose |
|------|----------|---------|
| `README.md` | Root | Project overview |
| `PROJECT_STRUCTURE.md` | Root | Complete structure guide |
| `PROJECT_TREE.txt` | Root | Visual directory tree |
| `KEY_FILES_REFERENCE.md` | Root | This file |
| `DEPLOYMENT.md` | Root | Deployment procedures |
| `DEPLOYMENT_QUICK_REFERENCE.md` | Root | Quick deployment guide |
| `PRODUCTION_DEPLOYMENT_GUIDE.md` | Root | Production deployment steps |
| `PRODUCTION_HARDENING_COMPLETE.md` | Root | Auto-scaling & backups |
| `GITHUB_ACTIONS_PATH_FIX.md` | Root | CI/CD path fixes |
| `GITHUB_ACTIONS_QUICK_FIX.md` | Root | Quick path fix reference |
| `ECS_TASK_DEFINITION_ROLE_FIX.md` | Root | Task definition role fix |
| `ECS_ROLE_QUICK_FIX.md` | Root | Quick role fix reference |
| `AWS_DEPLOYMENT_READINESS.md` | Root | AWS readiness checklist |
| `AWS_RDS_SETUP_REFERENCE.md` | Root | RDS setup guide |
| `EC2_DEPLOYMENT_CHECKLIST.md` | Root | EC2 deployment steps |

---

## 🧩 Backend Core Files

### Controllers (API Endpoints)
| File | Location | Purpose |
|------|----------|---------|
| `AuthController.cs` | `invmgmt.web/Controllers/` | Authentication endpoints |
| `UserController.cs` | `invmgmt.web/Controllers/` | User management |
| `*Controller.cs` | `invmgmt.web/Controllers/` | API endpoints |

### Models (Domain Entities)
| File | Location | Purpose |
|------|----------|---------|
| `User.cs` | `invmgmt.web/Models/` | User entity |
| `Role.cs` | `invmgmt.web/Models/` | Role entity |
| `Department.cs` | `invmgmt.web/Models/` | Department entity |
| `Category.cs` | `invmgmt.web/Models/` | Item category |
| `*.cs` | `invmgmt.web/Models/` | Other domain models |

### Services (Business Logic)
| File | Location | Purpose |
|------|----------|---------|
| `AuthService.cs` | `invmgmt.web/Services/` | Authentication logic |
| `IAuthService.cs` | `invmgmt.web/Services/` | Auth service interface |
| `*.cs` | `invmgmt.web/Services/` | Business logic |

### Repositories (Data Access)
| File | Location | Purpose |
|------|----------|---------|
| `UserRepository.cs` | `invmgmt.web/Repositories/` | User data access |
| `IUserRepository.cs` | `invmgmt.web/Repositories/` | User repository interface |
| `*.cs` | `invmgmt.web/Repositories/` | Data access |

### Utilities
| File | Location | Purpose |
|------|----------|---------|
| `PasswordUtils.cs` | `invmgmt.web/Utils/` | Password hashing & validation |
| `TraceIdEnricherMiddleware.cs` | `invmgmt.web/Utils/` | Request tracing |

---

## 🎨 Frontend Core Files

### Components
| Directory | Location | Purpose |
|-----------|----------|---------|
| `app/components/` | `Invmgmt-master/src/` | Reusable components |
| `app/pages/` | `Invmgmt-master/src/` | Page components |
| `app.component.ts` | `Invmgmt-master/src/app/` | Root component |

### Services
| Directory | Location | Purpose |
|-----------|----------|---------|
| `app/services/` | `Invmgmt-master/src/` | HTTP services |

### Models
| Directory | Location | Purpose |
|-----------|----------|---------|
| `app/models/` | `Invmgmt-master/src/` | TypeScript interfaces |

### Configuration
| File | Location | Purpose |
|------|----------|---------|
| `main.ts` | `Invmgmt-master/src/` | Bootstrap file |
| `index.html` | `Invmgmt-master/src/` | HTML entry point |
| `styles.css` | `Invmgmt-master/src/` | Global styles |
| `app-routing.module.ts` | `Invmgmt-master/src/app/` | Route definitions |
| `environment.ts` | `Invmgmt-master/src/environments/` | Dev environment |
| `environment.prod.ts` | `Invmgmt-master/src/environments/` | Prod environment |

---

## 🌍 Environment Variables

### Backend (.env / appsettings.json)
```
ConnectionStrings__DefaultConnection = "Host=...;Database=inventorydb;..."
ASPNETCORE_ENVIRONMENT = Production
ASPNETCORE_URLS = http://+:5000
Jwt__Key = [SECRET]
Jwt__Issuer = invmgmt
Jwt__Audience = invmgmt_user
ADMIN_EMAIL = admin@gmail.com
ADMIN_PASSWORD = [SECRET]
FRONTEND_URL = http://invmgmt-master.s3-website-us-east-1.amazonaws.com
```

### Frontend (environment.prod.ts)
```
production = true
apiUrl = "http://54.89.134.48:5000"
```

### Infrastructure (terraform variables)
```
aws_region = "us-east-1"
db_password = [SENSITIVE]
```

### GitHub Actions Secrets
```
AWS_ACCESS_KEY_ID = [SECRET]
AWS_SECRET_ACCESS_KEY = [SECRET]
AWS_ACCOUNT_ID = 396287094524
```

---

## 📊 Critical Paths

### Database
- **Connection String**: `appsettings.json` → `Program.cs`
- **Migrations**: `Migrations/` → `Program.cs` (startup)
- **RDS Instance**: Terraform `main.tf` → ECS Task Definition

### Authentication
- **JWT Setup**: `Program.cs` → `appsettings.json`
- **Password Hashing**: `PasswordUtils.cs` → Repositories/Services
- **Token Validation**: `Program.cs` (middleware) → Controllers

### Deployment
- **Docker Build**: `Dockerfile` → GitHub Actions
- **AWS Resources**: `terraform/` files → AWS
- **Task Definition**: `task-definition.json` → ECS
- **Frontend**: `Invmgmt-master/dist/` → S3

---

## ✅ File Checklist for Deployment

- [ ] `.env.prod` exists and has all secrets
- [ ] `task-definition.json` has correct AWS account ID
- [ ] `terraform/variables.tf` has correct region & password
- [ ] GitHub Secrets added (3 AWS credentials)
- [ ] `Dockerfile` exists in both backend & frontend
- [ ] `appsettings.json` has correct database name
- [ ] `.github/workflows/deploy.yml` is correct

---

## 🔍 Finding Specific Functionality

| What You Need | Where to Look |
|---------------|---------------|
| API Endpoints | `invmgmt.web/Controllers/` |
| Database Schema | `invmgmt.web/Models/` + `Migrations/` |
| User Authentication | `AuthService.cs` + `AuthController.cs` |
| Frontend Pages | `Invmgmt-master/src/app/pages/` |
| API Calls | `Invmgmt-master/src/app/services/` |
| Styling | `Invmgmt-master/src/styles.css` |
| AWS Configuration | `terraform/main.tf` |
| Build Process | `.github/workflows/deploy.yml` |
| Deployment Config | `docker-compose.yml` + `task-definition.json` |
| Secrets | `.env.prod` (not versioned) |
| Logs | `invmgmt.web/Logs/` (backend) + CloudWatch (AWS) |

---

## 📋 Quick Edit Guide

| Need to Change | File | Section |
|---|---|---|
| Database password | `terraform/variables.tf` | `db_password` default |
| App port | `Program.cs` | `ListenAnyIP(5000)` |
| API response format | `Controllers/*.cs` | Return statements |
| Frontend URL | `appsettings.json` | `FRONTEND_URL` env var |
| Auto-scaling limits | `terraform/main.tf` | `max_capacity`, `min_capacity` |
| Database backups | `terraform/main.tf` | `backup_retention_period` |
| CORS origins | `Program.cs` | `WithOrigins(...)` |
| ECS task memory | `task-definition.json` | `"memory": "512"` |
| Health check path | `task-definition.json` | `"command"` in healthCheck |
| ECR image name | `terraform/main.tf` | `aws_ecr_repository.app.name` |

---

**Generated**: June 17, 2026  
**Project Status**: Production Ready ✅
