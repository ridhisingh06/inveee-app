# Inveee App - Complete Project Structure

Generated: June 17, 2026

---

## 📁 Root Project Structure

```
inveee-app/
├── .github/                          # GitHub configuration
│   └── workflows/
│       ├── deploy.yml               # CI/CD pipeline for backend & frontend
│       └── terraform.yml            # Terraform deployment workflow
│
├── .vs/                             # Visual Studio cache
├── .vscode/                         # VS Code settings
│
├── invmgmt.web/                     # Backend (.NET 10 API)
│   ├── Controllers/
│   ├── Data/
│   ├── DTOs/
│   ├── Migrations/
│   ├── Models/
│   ├── Repositories/
│   ├── Services/
│   ├── Utils/
│   ├── Views/
│   ├── wwwroot/
│   ├── Logs/
│   ├── Properties/
│   │
│   ├── Program.cs                  # Application startup
│   ├── invmgmt.web.csproj          # Project file
│   ├── Dockerfile                  # Docker image config
│   ├── appsettings.json            # App configuration (prod)
│   ├── appsettings.Development.json # Dev configuration
│   ├── .dockerignore
│   │
│   └── [Test Scripts & SQL Files]
│       ├── test_auth.ps1
│       ├── test_reg.ps1
│       ├── query.sql
│       └── ... (other test/migration scripts)
│
├── Invmgmt-master/                  # Frontend (Angular 16+)
│   ├── src/
│   │   ├── app/                    # Angular components
│   │   ├── assets/                 # Images, icons, etc.
│   │   ├── environments/           # Environment configs
│   │   ├── index.html              # HTML entry point
│   │   ├── main.ts                 # Angular bootstrap
│   │   └── styles.css              # Global styles
│   │
│   ├── public/                     # Static assets
│   ├── node_modules/               # NPM dependencies
│   │
│   ├── package.json                # NPM configuration
│   ├── package-lock.json           # Dependency lock file
│   ├── angular.json                # Angular CLI config
│   ├── tsconfig.json               # TypeScript config
│   ├── tsconfig.app.json           # App TypeScript config
│   ├── tsconfig.spec.json          # Test TypeScript config
│   │
│   ├── Dockerfile                  # Docker image config
│   ├── nginx.default.conf          # Nginx configuration
│   ├── entrypoint.sh               # Docker entrypoint
│   ├── proxy.conf.json             # Angular proxy config
│   ├── .dockerignore
│   ├── .prettierrc                 # Code formatter config
│   └── .editorconfig               # Editor settings
│
├── invmgmt.web.Tests/              # Backend unit tests
│   └── [Test files]
│
├── terraform/                       # Infrastructure as Code (AWS)
│   ├── main.tf                     # Main infrastructure config
│   │   ├── VPC & Subnets
│   │   ├── RDS PostgreSQL
│   │   ├── ECS Fargate Cluster
│   │   ├── ECS Service & Task Definition
│   │   ├── ECR Repository
│   │   ├── Security Groups
│   │   ├── IAM Roles & Policies
│   │   ├── Auto-Scaling Config
│   │   ├── KMS Encryption
│   │   └── CloudWatch Logs
│   │
│   ├── variables.tf                # Variables & defaults
│   │   ├── aws_region: us-east-1
│   │   └── db_password
│   │
│   ├── outputs.tf                  # Output values (IPs, ARNs, etc.)
│   │
│   ├── terraform.tfstate           # Current infrastructure state
│   ├── terraform.tfstate.backup    # State backup
│   ├── .terraform.lock.hcl         # Terraform lock file
│   ├── .terraform/                 # Terraform cache
│   │
│   └── [Plan files]
│       ├── tfplan-prod
│       └── tfplan-prod2
│
├── scripts/                        # Utility scripts
│   └── [Build, deploy, diagnostic scripts]
│
├── .git/                           # Git repository
├── .gitignore                      # Git ignore rules
│
├── Configuration Files
│   ├── docker-compose.yml          # Local development setup
│   ├── .env.example                # Environment variables template
│   ├── .env.prod                   # Production env (gitignored)
│   ├── task-definition.json        # ECS task definition
│   │
│   └── Deployment & Reference Docs
│       ├── deploy.sh               # Shell deployment script
│       ├── deploy-remote.ps1       # PowerShell deployment
│       ├── diagnose.sh             # Diagnostic script
│       ├── README.md               # Project overview
│       ├── DEPLOYMENT.md           # Deployment guide
│       ├── DEPLOYMENT_QUICK_REFERENCE.md
│       ├── EC2_DEPLOYMENT_CHECKLIST.md
│       ├── PRODUCTION_DEPLOYMENT_GUIDE.md
│       ├── AWS_DEPLOYMENT_READINESS.md
│       ├── AWS_RDS_SETUP_REFERENCE.md
│       ├── PRODUCTION_HARDENING_COMPLETE.md
│       ├── GITHUB_ACTIONS_PATH_FIX.md
│       └── ECS_TASK_DEFINITION_ROLE_FIX.md
│
└── Build Output & Cache (ignored)
    ├── bin/                        # .NET build output
    ├── obj/                        # .NET object files
    ├── invmgmt.web_buildtmp/       # Temp build folder
    ├── node_modules/               # NPM packages
    ├── dist/                       # Angular build output
    └── [Log files]
        ├── *.log
        ├── *.stderr
        └── *.stdout
```

---

## 🔍 Detailed Component Structure

### Backend - invmgmt.web/

```
invmgmt.web/
├── Controllers/
│   ├── AuthController.cs
│   ├── InventoryController.cs
│   ├── UserController.cs
│   └── [Other API controllers]
│
├── Models/
│   ├── User.cs
│   ├── Role.cs
│   ├── Department.cs
│   ├── Category.cs
│   ├── InventoryItem.cs
│   └── [Other domain models]
│
├── DTOs/
│   ├── UserRegisterDto.cs
│   ├── UserLoginDto.cs
│   ├── InventoryItemDto.cs
│   └── [Other data transfer objects]
│
├── Data/
│   ├── AppDbContext.cs             # Entity Framework context
│   └── [Database configuration]
│
├── Repositories/
│   ├── IUserRepository.cs
│   ├── UserRepository.cs
│   ├── IInventoryRepository.cs
│   ├── InventoryRepository.cs
│   └── [Other repository interfaces & implementations]
│
├── Services/
│   ├── IAuthService.cs
│   ├── AuthService.cs
│   ├── IInventoryService.cs
│   ├── InventoryService.cs
│   └── [Other business logic services]
│
├── Utils/
│   ├── PasswordUtils.cs            # Password hashing
│   ├── TraceIdEnricherMiddleware.cs
│   └── [Helper utilities]
│
├── Migrations/
│   ├── [EF Core migration files]
│   └── AppDbContextModelSnapshot.cs
│
├── Views/
│   └── [Razor view files if used]
│
├── wwwroot/
│   ├── css/
│   ├── js/
│   ├── images/
│   ├── uploads/
│   │   └── personnel/              # User profile photos
│   └── [Static files]
│
├── Properties/
│   └── launchSettings.json
│
├── Program.cs                      # ASP.NET Core startup
├── Dockerfile                      # Multi-stage Docker build
├── invmgmt.web.csproj             # Project file with dependencies
├── appsettings.json               # Production settings
├── appsettings.Development.json   # Development settings
├── .dockerignore                  # Docker ignore rules
│
└── Logs/                          # Application logs
    └── [Daily log files]
```

### Frontend - Invmgmt-master/

```
Invmgmt-master/
├── src/
│   ├── app/
│   │   ├── components/            # Reusable components
│   │   ├── pages/                 # Page components
│   │   ├── services/              # HTTP services
│   │   ├── models/                # TypeScript interfaces
│   │   ├── app.component.ts       # Root component
│   │   └── app-routing.module.ts # Routing configuration
│   │
│   ├── assets/
│   │   ├── images/
│   │   ├── icons/
│   │   └── [Other static assets]
│   │
│   ├── environments/
│   │   ├── environment.ts         # Dev environment
│   │   └── environment.prod.ts    # Prod environment
│   │
│   ├── index.html                 # HTML entry point
│   ├── main.ts                    # Angular bootstrap
│   ├── styles.css                 # Global styles
│   └── [Other global configs]
│
├── public/                        # Static public assets
├── node_modules/                  # NPM packages (not versioned)
├── dist/                          # Compiled Angular app (ignored)
│   └── invmgmt-frontend/
│       └── browser/               # Browser build output
│
├── package.json                   # NPM script & dependencies
├── angular.json                   # Angular CLI configuration
├── tsconfig.json                  # TypeScript configuration
├── tsconfig.app.json
├── tsconfig.spec.json
│
├── Dockerfile                     # Multi-stage Docker build
├── nginx.default.conf             # Nginx reverse proxy config
├── entrypoint.sh                  # Docker entrypoint script
├── proxy.conf.json                # Angular dev proxy config
│
├── .prettierrc                    # Code formatting rules
├── .editorconfig                  # Editor settings
├── .dockerignore
└── README.md
```

### Infrastructure - terraform/

```
terraform/
├── main.tf
│   ├── AWS Provider Configuration
│   ├── VPC & Networking
│   │   ├── Default VPC
│   │   └── Subnets
│   ├── Security Groups
│   │   ├── ECS Security Group
│   │   └── RDS Security Group
│   ├── ECR Repository
│   ├── RDS PostgreSQL
│   │   ├── DB Instance
│   │   ├── Subnet Group
│   │   ├── KMS Encryption Key
│   │   └── Enhanced Monitoring
│   ├── ECS
│   │   ├── Cluster
│   │   ├── Task Definition
│   │   ├── Service
│   │   ├── Auto-Scaling Target
│   │   ├── CPU Scaling Policy
│   │   └── Memory Scaling Policy
│   ├── IAM Roles
│   │   ├── ECS Task Execution Role
│   │   ├── RDS Monitoring Role
│   │   └── Policies
│   └── CloudWatch Logs
│
├── variables.tf
│   ├── aws_region: us-east-1
│   └── db_password: (sensitive)
│
├── outputs.tf
│   └── [Output values for deployment]
│
├── terraform.tfstate              # Current AWS state
├── .terraform.lock.hcl            # Dependency lock
├── .terraform/                    # Downloaded plugins
└── [Plan files]
    ├── tfplan-prod
    └── tfplan-prod2
```

### GitHub Workflows - .github/workflows/

```
.github/workflows/
│
├── deploy.yml
│   ├── Triggers: push to main, pull requests
│   ├── Jobs:
│   │   ├── debug                  # Show directory structure
│   │   ├── build-backend          # .NET build & test
│   │   ├── build-frontend         # Angular build
│   │   └── deploy                 # ECR push + ECS deploy
│   ├── AWS Credential Configuration
│   ├── Frontend Deployment
│   │   ├── Build Angular app
│   │   └── Deploy to S3
│   ├── Backend Deployment
│   │   ├── Build Docker image
│   │   ├── Push to ECR
│   │   ├── Update ECS task definition
│   │   └── Deploy to ECS service
│   └── Deployment Summary
│
└── terraform.yml
    ├── Infrastructure deployment
    ├── Plan & apply Terraform changes
    └── State management
```

---

## 📊 Technology Stack

### Backend
- **Runtime**: .NET 10
- **Framework**: ASP.NET Core Web API
- **Database**: PostgreSQL 18.3
- **ORM**: Entity Framework Core
- **Authentication**: JWT (JSON Web Tokens)
- **Logging**: Serilog
- **Containerization**: Docker

### Frontend
- **Framework**: Angular 16+
- **Language**: TypeScript
- **Styling**: CSS
- **HTTP Client**: Angular HttpClient
- **Package Manager**: npm
- **Containerization**: Docker + Nginx

### Infrastructure
- **Cloud**: AWS (us-east-1)
- **Compute**: ECS Fargate
- **Database**: RDS PostgreSQL
- **Container Registry**: ECR
- **Infrastructure as Code**: Terraform
- **CI/CD**: GitHub Actions
- **Security**: IAM, KMS, Security Groups
- **Monitoring**: CloudWatch Logs
- **Scaling**: Application Auto Scaling

---

## 🚀 Deployment Architecture

```
GitHub Repository
    ↓
GitHub Actions (CI/CD)
    ├─→ Build Backend (.NET)
    ├─→ Build Frontend (Angular)
    └─→ Deploy
        ├─→ Push to ECR
        ├─→ Deploy to ECS
        └─→ Upload to S3

AWS Infrastructure
    ├─→ ECS Fargate (Backend)
    │   ├─ Task 1, 2, ... N
    │   └─ Auto-scaling (2-10 tasks)
    │
    ├─→ S3 (Frontend)
    │   └─ Static website
    │
    ├─→ RDS PostgreSQL (Database)
    │   ├─ 30-day backups
    │   ├─ KMS encryption
    │   └─ Enhanced monitoring
    │
    └─→ CloudWatch (Logs & Metrics)
```

---

## 📈 Project Statistics

| Component | Type | Count |
|-----------|------|-------|
| Controllers | C# Classes | ~5 |
| Services | C# Classes | ~10 |
| Repositories | C# Classes | ~8 |
| Models | C# Classes | ~10 |
| DTOs | C# Classes | ~15 |
| Angular Components | TypeScript | ~20+ |
| Tests | PowerShell Scripts | ~6 |
| Terraform Resources | IaC | ~25+ |
| GitHub Actions Jobs | CI/CD | 4 |
| Documentation Files | Markdown | 15+ |

---

## 🔐 Security Configuration

```
AWS Resources Protected By:
├── VPC Security Groups
│   ├── ECS: 80, 5000 (HTTP)
│   └── RDS: 5432 (PostgreSQL)
├── IAM Roles & Policies
├── KMS Encryption
│   ├── RDS database
│   └── EBS volumes
├── GitHub Secrets
│   ├── AWS_ACCESS_KEY_ID
│   ├── AWS_SECRET_ACCESS_KEY
│   └── AWS_ACCOUNT_ID
└── Environment Variables
    ├── Database connection strings
    ├── JWT secrets
    └── API configuration
```

---

## 📝 Key Files by Purpose

### Development
- `Program.cs` - Application startup
- `appsettings.Development.json` - Dev config
- `docker-compose.yml` - Local dev environment

### Database
- `Migrations/` - EF Core migrations
- `*.sql` - Database scripts
- `Data/AppDbContext.cs` - EF Core context

### Deployment
- `Dockerfile` (both backend & frontend) - Container images
- `terraform/main.tf` - AWS infrastructure
- `.github/workflows/deploy.yml` - CI/CD pipeline
- `task-definition.json` - ECS task config

### Configuration
- `appsettings.json` - App settings
- `angular.json` - Angular CLI config
- `package.json` - NPM dependencies
- `tsconfig.json` - TypeScript config
- `invmgmt.web.csproj` - C# project file

### Documentation
- `PRODUCTION_DEPLOYMENT_GUIDE.md` - Deployment procedures
- `GITHUB_ACTIONS_PATH_FIX.md` - CI/CD troubleshooting
- `ECS_TASK_DEFINITION_ROLE_FIX.md` - AWS configuration
- `README.md` - Project overview

---

## 🔄 Build & Deployment Flow

```
1. Code Push to GitHub (main branch)
   ↓
2. GitHub Actions Triggered
   ├─ debug job (verify paths)
   ├─ build-backend job (.NET build & test)
   ├─ build-frontend job (Angular build)
   ↓
3. Deploy Job (if main branch)
   ├─ Configure AWS credentials
   ├─ Build Docker image
   ├─ Push to ECR
   ├─ Register ECS task definition
   ├─ Update ECS service
   ├─ Deploy frontend to S3
   ↓
4. Application Running
   ├─ Backend: http://54.89.134.48:5000
   ├─ Frontend: S3 website
   └─ Database: RDS PostgreSQL
```

---

## ✅ Project Readiness

- ✅ Source control: Git/GitHub
- ✅ CI/CD: GitHub Actions
- ✅ Infrastructure: Terraform
- ✅ Containerization: Docker
- ✅ Cloud: AWS (us-east-1)
- ✅ Database: PostgreSQL with backups
- ✅ Monitoring: CloudWatch Logs
- ✅ Auto-scaling: ECS service scaling
- ✅ Security: IAM, KMS, security groups
- ✅ Documentation: Comprehensive guides

---

**Last Updated**: June 17, 2026  
**Project Status**: Production-Ready ✅
