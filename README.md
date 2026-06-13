# InvMgmt - Full Stack Inventory Management System

## Overview

InvMgmt is a comprehensive inventory management application built with:

- **Frontend**: Angular 21 + Nginx
- **Backend**: ASP.NET Core 10 + PostgreSQL
- **Infrastructure**: Docker, Docker Compose, AWS EC2, Terraform

## Project Structure

```
invmgmt-app/
├── Invmgmt-master/              # Angular Frontend
│   ├── Dockerfile
│   ├── nginx.default.conf
│   ├── src/
│   ├── angular.json
│   └── package.json
├── invmgmt.web/                 # ASP.NET Core Backend
│   ├── Dockerfile
│   ├── Program.cs
│   ├── Controllers/
│   ├── Services/
│   ├── Models/
│   └── invmgmt.web.csproj
├── invmgmt.web.Tests/           # Backend Unit Tests
├── terraform/                   # AWS Infrastructure as Code
│   ├── main.tf
│   ├── variables.tf
│   └── outputs.tf
├── scripts/                     # Testing & Utility Scripts
├── docker-compose.yml           # Multi-container orchestration
├── DEPLOYMENT.md                # Detailed deployment guide
├── EC2_DEPLOYMENT_CHECKLIST.md  # Step-by-step EC2 setup
├── deploy.sh                    # Automated deployment script
└── diagnose.sh                  # Diagnostic tool

```

## Quick Start (Local Development)

### Prerequisites
- Node.js 20+
- .NET 10 SDK
- PostgreSQL 16
- Docker & Docker Compose (optional)

### Setup Frontend

```bash
cd Invmgmt-master
npm install
npm start
# Opens at http://localhost:4200
```

### Setup Backend

```bash
cd invmgmt.web
dotnet restore
dotnet run
# Runs at http://localhost:5000
```

### Setup with Docker Compose

```bash
# Build and start all services
docker-compose up -d

# Check status
docker-compose ps

# View logs
docker-compose logs -f

# Access services
# Frontend: http://localhost
# Backend:  http://localhost:5000
# Database: localhost:5432 (internal only)
```

---

## Production Deployment (AWS EC2)

### Quick Deployment (Automated)

From your local machine:

```powershell
# Windows
.\deploy-remote.ps1 -InstanceIP 100.55.99.251 -KeyPath "C:\path\to\key.pem"
```

```bash
# Mac/Linux
ssh -i ~/key.pem ubuntu@100.55.99.251 'bash -s' < deploy.sh
```

### Manual Deployment

See [EC2_DEPLOYMENT_CHECKLIST.md](EC2_DEPLOYMENT_CHECKLIST.md) for step-by-step instructions.

### Full Documentation

- **[DEPLOYMENT.md](DEPLOYMENT.md)** - Comprehensive deployment guide
- **[EC2_DEPLOYMENT_CHECKLIST.md](EC2_DEPLOYMENT_CHECKLIST.md)** - EC2 setup checklist

---

## Running the Application

### Docker Compose (Recommended for Production)

```bash
# Environment setup
cp .env.prod .env
nano .env  # Edit production values

# Start services
docker-compose up -d --build

# Verify deployment
docker-compose ps
curl http://localhost:5000/health
```

### Direct Commands (Development)

```bash
# Terminal 1: Frontend
cd Invmgmt-master
npm start

# Terminal 2: Backend
cd invmgmt.web
dotnet run

# Terminal 3: Database (if not using Docker)
# Setup PostgreSQL connection string in appsettings.json
```

---

## Configuration

### Environment Variables (.env)

**Required for production:**

```env
POSTGRES_PASSWORD=YourSecurePassword
JWT_KEY=your-jwt-key-min-32-chars
ADMIN_PASSWORD=admin-password
ASPNETCORE_ENVIRONMENT=Production
```

Generate secure JWT key:
```bash
openssl rand -base64 32
```

### Database Connection

The connection string is configured in:
- **Production**: Environment variable `ConnectionStrings__DefaultConnection`
- **Development**: `appsettings.Development.json`

Format:
```
Host=postgres;Port=5432;Database=postgres;Username=postgres;Password=...
```

### API Configuration

Frontend API URL is configured in `Invmgmt-master/src/environments/environment.ts`:
- **Development**: Proxied via Nginx at `/api`
- **Production**: Direct to `http://100.55.99.251/api`

---

## API Endpoints

### Health & Status
- `GET /health` - Service health check
- `GET /` - API info

### Authentication
- `POST /api/auth/register` - Register new user
- `POST /api/auth/login` - Login user
- `POST /api/auth/refresh` - Refresh JWT token

### Inventory Management
- `GET /api/items` - List inventory items
- `POST /api/items` - Create item
- `PUT /api/items/{id}` - Update item
- `DELETE /api/items/{id}` - Delete item

### Requests
- `GET /api/requests` - List all requests
- `POST /api/requests` - Create request
- `PUT /api/requests/{id}` - Update request

### Admin
- `GET /api/admin/users` - List users
- `POST /api/admin/approve-user/{id}` - Approve pending user
- `GET /api/admin/pending-users` - List pending approvals

**Full API documentation**: http://localhost:5000/swagger

---

## Monitoring & Troubleshooting

### Check Deployment Health

Run the diagnostic tool:
```bash
chmod +x diagnose.sh
./diagnose.sh
```

### View Logs

```bash
# All services
docker-compose logs -f

# Specific service
docker-compose logs -f backend
docker-compose logs -f frontend
docker-compose logs -f postgres
```

### Common Issues

**1. Build Fails - Dockerfile Not Found**
```bash
# WRONG: Builds from project root
docker build -t invmgmt-app .

# CORRECT: Build from subdirectory
docker build -t invmgmt-backend ./invmgmt.web
docker build -t invmgmt-frontend ./Invmgmt-master

# OR: Use docker-compose (handles paths)
docker-compose up -d --build
```

**2. Backend Container Exits**
```bash
# Check logs
docker-compose logs backend

# Restart with dependencies
docker-compose restart postgres
sleep 10
docker-compose restart backend
```

**3. Cannot Connect to API**
```bash
# Test backend health
curl http://localhost:5000/health

# Check Nginx proxy
docker exec invmgmt-frontend curl http://backend:5000/health
```

**4. Database Connection Error**
```bash
# Verify PostgreSQL is running
docker-compose ps postgres

# Check connection string in .env
grep ConnectionStrings .env

# Verify credentials
docker-compose exec postgres psql -U postgres -c "\dt"
```

For more troubleshooting, see [DEPLOYMENT.md](DEPLOYMENT.md#common-issues--fixes)

---

## Security

✅ **Best Practices Implemented**:
- JWT token authentication
- Password hashing with BCrypt
- CORS policy configuration
- Environment secrets (not in git)
- Database connection pooling
- SQL injection prevention (EF Core)

⚠️ **Before Production Deployment**:
- [ ] Change all default passwords in `.env`
- [ ] Generate strong `JWT_KEY` (min 32 chars)
- [ ] Enable HTTPS/SSL certificate
- [ ] Configure firewall rules
- [ ] Setup database backups
- [ ] Review security group settings
- [ ] Enable database SSL connections

---

## Infrastructure (Terraform)

Deploy to AWS using Terraform:

```bash
cd terraform

# Review changes
terraform plan

# Apply infrastructure
terraform apply

# Get outputs (IP, DNS, etc.)
terraform output
```

Terraform creates:
- EC2 instance (Ubuntu 22.04)
- Security groups (SSH, HTTP, HTTPS)
- VPC & networking
- IAM roles (optional)

---

## Testing

### Backend Tests

```bash
cd invmgmt.web.Tests
dotnet test
```

### API Integration Tests

```bash
# Test registration
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"username":"test","email":"test@test.com","password":"Test123!"}'

# Test login
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@example.com","password":"AdminPassword123!"}'
```

---

## Database

### PostgreSQL Administration

```bash
# Connect to database
docker-compose exec postgres psql -U postgres

# Useful commands:
\dt                    # List tables
\d table_name          # Describe table
SELECT * FROM users;   # Query

# Backup
docker-compose exec postgres pg_dump -U postgres postgres > backup.sql

# Restore
docker-compose exec -T postgres psql -U postgres postgres < backup.sql
```

### Migrations

```bash
# Create migration
dotnet ef migrations add MigrationName

# Apply migrations
dotnet ef database update
```

---

## Performance Optimization

- Angular: Tree shaking, AOT compilation, lazy loading
- Backend: Connection pooling (5-20 connections), response caching
- Database: Indexes on frequently queried columns
- Nginx: Gzip compression, static asset caching

---

## Support & Contributing

- Issues: https://github.com/ridhisingh06/inveee-app/issues
- Pull Requests: https://github.com/ridhisingh06/inveee-app/pulls

---

## License

© 2026 InvMgmt. All rights reserved.

---

## Key Files Reference

| File | Purpose |
|------|---------|
| `docker-compose.yml` | Orchestrates all services (Postgres, Backend, Frontend) |
| `.env.prod` | Production environment template |
| `deploy.sh` | Automated EC2 deployment script |
| `deploy-remote.ps1` | Remote deployment from Windows |
| `diagnose.sh` | Deployment diagnostic tool |
| `DEPLOYMENT.md` | Detailed deployment & troubleshooting guide |
| `EC2_DEPLOYMENT_CHECKLIST.md` | Step-by-step EC2 setup checklist |

---

## Version History

- **2026-06-14**: Added comprehensive Docker deployment setup
- **Previous**: Initial project setup

