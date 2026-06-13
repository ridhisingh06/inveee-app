# Full-Stack Docker Deployment Guide

## Project Structure

```
invmgmt-app/
├── Invmgmt-master/              # Angular Frontend
│   ├── Dockerfile               # Multi-stage Node.js + Nginx build
│   ├── nginx.default.conf       # Nginx configuration
│   └── package.json
├── invmgmt.web/                 # .NET Backend API
│   ├── Dockerfile               # Multi-stage .NET build
│   ├── Program.cs               # ASP.NET Core entry point
│   └── *.csproj
├── docker-compose.yml           # Orchestrates all services
├── .env.prod                    # Production environment template
├── deploy.sh                    # Automated deployment script
└── README.md
```

## Services Architecture

### 1. PostgreSQL Database
- **Container**: `invmgmt-postgres`
- **Image**: `postgres:16-alpine`
- **Port**: 5432 (internal only, exposed through backend)
- **Volume**: `postgres_data` (persistent storage)
- **Health Check**: Automatic recovery on failure

### 2. ASP.NET Core Backend
- **Container**: `invmgmt-backend`
- **Image**: Built from `invmgmt.web/Dockerfile`
- **Port**: 5000
- **Features**:
  - .NET 10 runtime
  - JWT authentication
  - PostgreSQL connection pooling
  - Swagger/OpenAPI docs at `/swagger`
  - Health check at `/health`

### 3. Angular Frontend (Nginx)
- **Container**: `invmgmt-frontend`
- **Image**: Built from `Invmgmt-master/Dockerfile`
- **Port**: 80
- **Features**:
  - Node.js 20 build stage
  - Nginx Alpine serving
  - Static content optimization
  - Reverse proxy configured for API calls

---

## Deployment Steps (EC2)

### Prerequisites
```bash
# On EC2 instance (Ubuntu 22.04+)
sudo apt update
sudo apt install -y docker.io docker-compose git
sudo usermod -aG docker ubuntu
newgrp docker  # Apply group changes
```

### Quick Start (Automated)

#### From Your Local Machine (Windows/Mac/Linux)

**Option 1: Using PowerShell (Windows)**
```powershell
.\deploy-remote.ps1 -InstanceIP 100.55.99.251 -KeyPath "C:\Users\Singh\Downloads\inveeemgmt.pem"
```

**Option 2: Using Bash (Mac/Linux)**
```bash
chmod +x deploy.sh
scp -i ~/inveeemgmt.pem deploy.sh ubuntu@100.55.99.251:~/
ssh -i ~/inveeemgmt.pem ubuntu@100.55.99.251 "bash ~/deploy.sh"
```

#### Manually on EC2 Instance

```bash
# 1. Clone repository
git clone https://github.com/ridhisingh06/inveee-app.git
cd inveee-app

# 2. Create environment file
cp .env.prod .env

# 3. Edit .env with production values
nano .env
# Change: POSTGRES_PASSWORD, JWT_KEY, ADMIN_PASSWORD

# 4. Build and start services
docker-compose up -d

# 5. Verify deployment
docker-compose ps
docker-compose logs -f
```

---

## Configuration

### Environment Variables (.env)

**Critical Variables (MUST CHANGE in Production):**

```env
# Database
POSTGRES_PASSWORD=ChangeMe123!          # Use strong password
POSTGRES_USER=postgres
POSTGRES_DB=postgres

# JWT Security
JWT_KEY=GenerateRandomString32CharsMin   # Use: openssl rand -base64 32
JWT_ISSUER=invmgmt-api
JWT_AUDIENCE=invmgmt-frontend

# Admin Account
ADMIN_EMAIL=admin@example.com
ADMIN_PASSWORD=AdminPassword123!

# Environment
ASPNETCORE_ENVIRONMENT=Production
```

**Generate Secure JWT Key:**
```bash
# On EC2 or local machine
openssl rand -base64 32
```

### Nginx Configuration (Frontend)
- Location: `Invmgmt-master/nginx.default.conf`
- API Proxy: Routes `/api/*` calls to backend at `http://backend:5000`
- Static Files: Serves Angular build from `/usr/share/nginx/html`

---

## Common Issues & Fixes

### Issue 1: "Dockerfile not found" or Build Fails

**Symptom**: `Error: no such file or directory: Dockerfile`

**Solution**:
```bash
# WRONG - from root directory
docker build -t invmgmt-backend .

# CORRECT - from correct directory
docker build -t invmgmt-backend ./invmgmt.web
# OR use docker-compose (handles paths automatically)
docker-compose up --build
```

**Why**: Dockerfile locations:
- `./Invmgmt-master/Dockerfile` (frontend)
- `./invmgmt.web/Dockerfile` (backend)

---

### Issue 2: Repository Already Exists on EC2

**Symptom**: `fatal: destination path '/home/ubuntu/inveee-app' already exists`

**Solution**:
```bash
# Option 1: Update existing repo
cd /home/ubuntu/inveee-app
git pull origin main
docker-compose down -v
docker-compose up -d --build

# Option 2: Clean start
rm -rf /home/ubuntu/inveee-app
git clone https://github.com/ridhisingh06/inveee-app.git
cd inveee-app
docker-compose up -d
```

---

### Issue 3: Docker Build Fails with Network Errors

**Symptom**: `E: Failed to fetch packages` or `npm install` fails

**Solution**:
```bash
# Rebuild with clean cache
docker-compose down -v
docker system prune -a --volumes
docker-compose build --no-cache
docker-compose up -d
```

---

### Issue 4: Backend Container Exits Immediately

**Symptom**: `docker ps` shows backend not running

**Debug**:
```bash
# Check logs
docker-compose logs backend

# Common reasons:
# 1. Database connection failed → Wait for postgres healthcheck
# 2. Missing environment variables → Check .env file
# 3. Port conflict → Change ASPNETCORE_URLS
```

**Fix**:
```bash
# Restart with dependency check
docker-compose restart postgres
sleep 10
docker-compose restart backend
```

---

### Issue 5: Frontend Shows "Cannot Connect to API"

**Symptom**: Browser console: `Failed to fetch from http://...`

**Debug**:
```bash
# Check if backend is running
curl http://localhost:5000/health

# Check Nginx logs
docker-compose logs frontend

# Verify Nginx proxy configuration
docker exec invmgmt-frontend cat /etc/nginx/conf.d/default.conf
```

**Fix**: Ensure backend service is named `backend` in docker-compose.yml (Nginx uses DNS service discovery)

---

### Issue 6: Database Connection String Error

**Symptom**: Backend logs show `Host not found` or `connection refused`

**Debug**:
```bash
# Verify PostgreSQL is running
docker-compose ps postgres

# Test connection from backend container
docker-compose exec backend bash
apt-get install -y postgresql-client
psql -h postgres -U postgres -d postgres -c "\dt"
```

**Fix**: Use service name `postgres` (not localhost), and wait for healthcheck:
```bash
# In docker-compose.yml, backend depends_on has:
depends_on:
  postgres:
    condition: service_healthy
```

---

## Monitoring & Maintenance

### View Logs
```bash
# All services
docker-compose logs -f

# Specific service
docker-compose logs -f backend
docker-compose logs -f frontend
docker-compose logs -f postgres

# Last 50 lines
docker-compose logs --tail 50 backend
```

### Check Service Status
```bash
# Running containers
docker-compose ps

# Resource usage
docker stats

# Inspect service
docker-compose exec backend curl http://localhost:5000/health
```

### Database Operations
```bash
# Connect to PostgreSQL
docker-compose exec postgres psql -U postgres

# Backup database
docker-compose exec postgres pg_dump -U postgres postgres > backup.sql

# Restore database
docker-compose exec -T postgres psql -U postgres postgres < backup.sql
```

### Restart Services
```bash
# Restart specific service
docker-compose restart backend

# Restart all
docker-compose restart

# Restart with rebuild
docker-compose down
docker-compose up -d --build
```

---

## Performance Optimization

### Docker Memory Limits
Edit `docker-compose.yml`:
```yaml
services:
  backend:
    deploy:
      resources:
        limits:
          cpus: '1'
          memory: 2G
        reservations:
          cpus: '0.5'
          memory: 1G
```

### Database Connection Pooling
In `.env`:
```env
# Already configured in Program.cs
# Npgsql: Minimum=5, Maximum=20
# Connection Idle Lifetime=30 seconds
```

### Nginx Caching
In `Invmgmt-master/nginx.default.conf`:
```nginx
# Cache static assets for 1 year
location ~* \.(js|css|png|jpg|jpeg|gif|ico|svg)$ {
    expires 1y;
    add_header Cache-Control "public, immutable";
}
```

---

## Security Best Practices

✅ **DO**:
- Generate strong JWT_KEY: `openssl rand -base64 32`
- Use environment variables for secrets (never hardcode)
- Keep `.env` file out of git (already in .gitignore)
- Use HTTPS in production (set up SSL/TLS reverse proxy)
- Regularly update Docker images: `docker-compose pull && docker-compose up -d`
- Monitor logs for suspicious activity
- Use PostgreSQL strong passwords

❌ **DON'T**:
- Commit `.env` to git
- Use default passwords in production
- Expose database port 5432 to public internet
- Run as root in containers
- Disable CORS/authentication for testing

---

## Cleanup & Removal

```bash
# Stop services (containers persist)
docker-compose stop

# Remove containers
docker-compose down

# Remove everything including database
docker-compose down -v

# Remove all images
docker rmi invmgmt-backend:latest invmgmt-frontend:latest

# Clean entire Docker system
docker system prune -a --volumes
```

---

## Useful Links

- **Health Check**: http://100.55.99.251:5000/health
- **API Documentation**: http://100.55.99.251:5000/swagger
- **Frontend**: http://100.55.99.251/
- **PostgreSQL**: `postgres://postgres:password@100.55.99.251:5432/postgres` (internal only)

---

## Support

For issues, check:
1. Logs: `docker-compose logs backend`
2. Database: `docker-compose ps postgres`
3. Network: `docker network ls` and `docker network inspect inveee-app_invmgmt-network`
4. GitHub: https://github.com/ridhisingh06/inveee-app

