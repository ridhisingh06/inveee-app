# Project Folder Structure - Executive Summary

**Project**: Inveee App - Inventory Management System  
**Architecture**: Microservices (Backend API + Frontend SPA)  
**Deployment**: AWS (ECS, RDS, S3, ECR)  
**Generated**: June 17, 2026

---

## 📊 High-Level Overview

```
inveee-app/
├── Backend API (.NET 10)          → invmgmt.web/
├── Frontend SPA (Angular 16+)     → Invmgmt-master/
├── Infrastructure as Code         → terraform/
├── CI/CD Pipeline                 → .github/workflows/
└── Configuration & Docs           → Root & documentation/
```

---

## 🗂️ Main Folders (First Level)

| Folder | Size | Purpose | Type |
|--------|------|---------|------|
| `invmgmt.web/` | ~50MB | Backend API | .NET Core |
| `Invmgmt-master/` | ~200MB | Frontend App | Angular 16+ |
| `terraform/` | ~10MB | Infrastructure | IaC (Terraform) |
| `.github/` | <1MB | CI/CD Pipelines | GitHub Actions |
| `scripts/` | <1MB | Utilities | Shell/PowerShell |
| `node_modules/` | ~500MB | NPM deps (ignored) | Generated |
| `inveee-app/` | TBD | Nested repo | Submodule? |
| `invmgmt.web.Tests/` | ~20MB | Unit Tests | .NET |
| `invmgmt.web_buildtmp/` | ~100MB | Build cache | Generated |

---

## 📈 File Count by Type

| Type | Count | Location |
|------|-------|----------|
| C# Files | ~50 | `invmgmt.web/` |
| TypeScript Files | ~25 | `Invmgmt-master/src/app/` |
| Configuration Files | ~20 | Root, `terraform/`, `Invmgmt-master/` |
| SQL Scripts | ~8 | `invmgmt.web/` |
| Terraform Files | ~5 | `terraform/` |
| Docker Files | 2 | `invmgmt.web/`, `Invmgmt-master/` |
| GitHub Workflow Files | 2 | `.github/workflows/` |
| Documentation Files | 15+ | Root |
| Test Scripts | 6 | `invmgmt.web/` |

---

## 🎯 Key Directories

### Backend (invmgmt.web/)
```
Controllers/     → API endpoints (5+ files)
Models/          → Domain entities (10+ files)
Services/        → Business logic (8+ files)
Repositories/    → Data access (6+ files)
DTOs/            → Data transfer objects (15+ files)
Data/            → EF Core context
Migrations/      → Database migrations
Utils/           → Helper utilities
Logs/            → Application logs
```

### Frontend (Invmgmt-master/src/)
```
app/
├── components/  → Reusable UI components
├── pages/       → Page components
├── services/    → HTTP services
└── models/      → TypeScript interfaces

assets/         → Images, icons
environments/   → Configuration files
styles.css      → Global styling
index.html      → HTML entry point
main.ts         → Bootstrap file
```

### Infrastructure (terraform/)
```
main.tf         → AWS resources (450+ lines)
                  ├── VPC & Security
                  ├── RDS Database
                  ├── ECS & ECR
                  ├── Auto-Scaling
                  ├── IAM Roles
                  └── Monitoring

variables.tf    → Input variables
outputs.tf      → Output values
.terraform/     → Downloaded plugins
```

### CI/CD (.github/workflows/)
```
deploy.yml      → Main pipeline
                  ├── build-backend
                  ├── build-frontend
                  └── deploy (ECR → ECS → S3)

terraform.yml   → Infrastructure pipeline
```

---

## 📝 File Organization Strategy

### By Function
- **Controllers** - API endpoints
- **Models** - Database/domain objects
- **Services** - Business logic
- **Repositories** - Data access
- **DTOs** - API request/response objects
- **Utils** - Helper functions

### By Layer
1. **Controller Layer** - HTTP endpoints
2. **Service Layer** - Business logic
3. **Repository Layer** - Database access
4. **Data Layer** - EF Core context

### By Concern
- **Auth** - Controllers, Services, Utils
- **Inventory** - Controllers, Services, Repositories, Models
- **User Management** - Similar structure
- **Database** - Migrations, Models, Repositories

---

## 🔄 Data Flow Architecture

```
User Request
    ↓
API Controller (invmgmt.web/Controllers/)
    ↓
Service (invmgmt.web/Services/)
    ↓
Repository (invmgmt.web/Repositories/)
    ↓
Entity Framework (invmgmt.web/Data/)
    ↓
PostgreSQL Database (AWS RDS)
```

---

## 🚀 Deployment Architecture

```
GitHub Repository
    ↓
GitHub Actions (.github/workflows/deploy.yml)
    ├─ Build Backend (invmgmt.web/) → Docker image
    ├─ Build Frontend (Invmgmt-master/) → Angular bundle
    └─ Deploy
        ├─ Push Docker image → ECR
        ├─ Update ECS task definition
        ├─ Deploy to ECS Fargate
        ├─ Upload Angular bundle → S3
        └─ Update frontend website

AWS Infrastructure (terraform/)
    ├─ ECS Fargate (backend API)
    ├─ S3 (frontend website)
    ├─ RDS PostgreSQL (database)
    ├─ ECR (container registry)
    ├─ CloudWatch (logging)
    └─ Auto-scaling (dynamic tasks)
```

---

## 💾 Storage & Size Estimates

| Component | Size | Storage Type |
|-----------|------|--------------|
| Source Code | ~50MB | Git |
| Node Modules | ~500MB | Local/ignored |
| .NET Build | ~100MB | Local/ignored |
| Database | ~100MB | RDS |
| Backend Image | ~115MB | ECR |
| Frontend Build | ~5MB | S3 |
| Logs | Growing | CloudWatch |
| Terraform State | ~1MB | Terraform state file |

---

## 🔐 Security Structure

```
Secrets (NOT versioned)
├── .env.prod              → Database password
├── GitHub Secrets         → AWS credentials
└── appsettings.*.json    → Production config

Public (Versioned)
├── .env.example          → Template
├── appsettings.json      → Default config
└── terraform/*.tf        → Infrastructure
```

---

## 📚 Documentation Map

| Document | Purpose |
|----------|---------|
| `README.md` | Project overview |
| `PROJECT_STRUCTURE.md` | Complete structure guide |
| `KEY_FILES_REFERENCE.md` | File quick reference |
| `DEPLOYMENT.md` | How to deploy |
| `PRODUCTION_HARDENING_COMPLETE.md` | Production config |
| `GITHUB_ACTIONS_PATH_FIX.md` | CI/CD troubleshooting |
| `ECS_TASK_DEFINITION_ROLE_FIX.md` | AWS role setup |
| `AWS_DEPLOYMENT_READINESS.md` | AWS checklist |

---

## ✅ Project Readiness Checklist

- ✅ **Source Control**: Git/GitHub organized
- ✅ **Folder Structure**: Clean separation of concerns
- ✅ **Configuration**: Environment-based settings
- ✅ **Database**: EF Core migrations organized
- ✅ **Frontend**: Angular modular structure
- ✅ **Infrastructure**: Terraform IaC organized
- ✅ **CI/CD**: GitHub Actions workflows
- ✅ **Documentation**: Comprehensive guides
- ✅ **Security**: Secrets management in place
- ✅ **Testing**: Test scripts included

---

## 🎯 Typical Development Workflow

1. **Clone Repository**
   ```bash
   git clone https://github.com/ridhisingh06/inveee-app.git
   cd inveee-app/inveee-app
   ```

2. **Backend Development**
   - Edit: `invmgmt.web/` files
   - Test: Run test scripts
   - Run: `dotnet run` or Docker

3. **Frontend Development**
   - Edit: `Invmgmt-master/src/` files
   - Run: `npm start` or `ng serve`
   - Build: `npm run build`

4. **Infrastructure Updates**
   - Edit: `terraform/main.tf`
   - Plan: `terraform plan`
   - Apply: `terraform apply`

5. **Commit & Push**
   - Stage changes
   - Commit: `git commit -m "..."`
   - Push: `git push origin main`
   - GitHub Actions auto-deploys

---

## 🔍 Quick File Lookup

| I need to... | Look in... |
|--------------|-----------|
| Add API endpoint | `invmgmt.web/Controllers/` |
| Change business logic | `invmgmt.web/Services/` |
| Modify database query | `invmgmt.web/Repositories/` |
| Add UI component | `Invmgmt-master/src/app/components/` |
| Change frontend styling | `Invmgmt-master/src/styles.css` |
| Update AWS resources | `terraform/main.tf` |
| Modify CI/CD pipeline | `.github/workflows/deploy.yml` |
| Change app config | `appsettings.json` |
| Add database migration | `invmgmt.web/Migrations/` |

---

## 📊 Statistics

- **Total Files**: 200+
- **Languages**: C#, TypeScript, HCL, YAML, SQL
- **Backend Classes**: ~50
- **Frontend Components**: ~25
- **Terraform Resources**: ~25
- **CI/CD Jobs**: 4
- **Documentation Files**: 15+
- **Git Commits**: 50+
- **Developers**: 1 (Ridhi Singh)
- **Lines of Code**: ~10,000+

---

## 🎓 Learning Resources in Project

1. **For .NET Developers**
   - ASP.NET Core setup: `Program.cs`
   - EF Core migrations: `Migrations/`
   - Repository pattern: `Repositories/`
   - Service layer: `Services/`

2. **For Angular Developers**
   - Angular setup: `angular.json`
   - Component structure: `src/app/`
   - Services: `src/app/services/`
   - Routing: `app-routing.module.ts`

3. **For DevOps Engineers**
   - Terraform IaC: `terraform/main.tf`
   - GitHub Actions: `.github/workflows/`
   - Docker setup: `Dockerfile`
   - ECS configuration: `task-definition.json`

---

## 🚀 Next Steps

1. **For Development**
   - Clone the repository
   - Set up local environment (`.env` file)
   - Run `docker-compose up` for local development
   - Start backend and frontend servers

2. **For Deployment**
   - Ensure AWS credentials are configured
   - Update Terraform variables if needed
   - Run `terraform apply` for infrastructure
   - Push to main branch to trigger GitHub Actions

3. **For Maintenance**
   - Review logs in `invmgmt.web/Logs/` and CloudWatch
   - Monitor auto-scaling metrics
   - Check database backups (30-day retention)
   - Review documentation for updates

---

## 📞 Key Contacts & References

- **Repository**: https://github.com/ridhisingh06/inveee-app
- **Production API**: http://54.89.134.48:5000
- **Frontend**: https://invmgmt-master.s3-website-us-east-1.amazonaws.com
- **Database**: RDS PostgreSQL (us-east-1)
- **Account ID**: 396287094524

---

**Status**: ✅ Production Ready  
**Last Updated**: June 17, 2026  
**Project Lead**: Ridhi Singh
