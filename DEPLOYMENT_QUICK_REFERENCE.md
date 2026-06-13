# EC2 Deployment Quick Reference Card

## One-Command Deployment (from your local machine)

### Windows (PowerShell)
```powershell
cd d:\inveee-app
.\deploy-remote.ps1 -InstanceIP 100.55.99.251 -KeyPath "C:\Users\Singh\Downloads\inveeemgmt.pem"
```

### Mac/Linux (Bash)
```bash
cd ~/inveee-app
ssh -i ~/inveeemgmt.pem ubuntu@100.55.99.251 'bash -s' < deploy.sh
```

---

## If Automated Deploy Fails: Manual Steps

### Step 1: SSH to EC2
```bash
ssh -i key.pem ubuntu@100.55.99.251
```

### Step 2: Install Docker (one-time)
```bash
sudo apt update && sudo apt install -y docker.io docker-compose git
sudo usermod -aG docker ubuntu
newgrp docker
```

### Step 3: Clone Repository
```bash
git clone https://github.com/ridhisingh06/inveee-app.git
cd inveee-app
```

### Step 4: Configure Environment
```bash
cp .env.prod .env
nano .env  # Edit: POSTGRES_PASSWORD, JWT_KEY, ADMIN_PASSWORD
```

### Step 5: Start Services
```bash
docker-compose up -d --build
# Wait 2-3 minutes for builds to complete
```

### Step 6: Verify Deployment
```bash
docker-compose ps              # All should show "Up"
curl http://localhost:5000/health
# Should return: {"status":"healthy",...}
```

---

## Access Your Application

| Component | URL | Purpose |
|-----------|-----|---------|
| **Frontend** | http://100.55.99.251 | Web application |
| **Backend API** | http://100.55.99.251:5000 | REST API |
| **API Docs** | http://100.55.99.251:5000/swagger | Swagger/OpenAPI |
| **Health Check** | http://100.55.99.251:5000/health | Service status |

---

## Verify All Services Running

```bash
# Quick status check
docker-compose ps

# Expected output:
#   NAME                STATE              PORTS
#   invmgmt-postgres    Up (healthy)       5432/tcp
#   invmgmt-backend     Up (healthy)       5000/tcp
#   invmgmt-frontend    Up                 80/tcp
```

---

## Common Troubleshooting

### Services not starting?
```bash
# View logs
docker-compose logs -f

# Rebuild from scratch
docker-compose down -v
docker system prune -a
docker-compose up -d --build
```

### Cannot connect to API?
```bash
# Test backend directly
docker-compose exec backend curl http://localhost:5000/health

# Check backend logs
docker-compose logs backend

# Common issue: Still building, wait 2-3 minutes
```

### Database connection error?
```bash
# Verify PostgreSQL is ready
docker-compose ps postgres
# Should show: healthy

# If not healthy, restart
docker-compose restart postgres
sleep 10
docker-compose restart backend
```

### Container exited unexpectedly?
```bash
# Get detailed logs
docker-compose logs postgres
docker-compose logs backend

# Check .env variables
cat .env | grep -E "POSTGRES_PASSWORD|JWT_KEY"
```

---

## Key Environment Variables

Must configure in `.env`:

| Variable | Example | Description |
|----------|---------|-------------|
| `POSTGRES_PASSWORD` | `MySecure123!` | Database password (REQUIRED) |
| `JWT_KEY` | `abc123...` | API auth key, min 32 chars (REQUIRED) |
| `ADMIN_PASSWORD` | `AdminPass123!` | Admin account password |
| `ADMIN_EMAIL` | `admin@example.com` | Admin login email |
| `ASPNETCORE_ENVIRONMENT` | `Production` | Deployment mode |

---

## Daily Operations Commands

```bash
# View all logs
docker-compose logs -f

# Check status
docker-compose ps

# Restart backend (after code changes)
docker-compose restart backend

# Restart all services
docker-compose restart

# Stop services (data persists)
docker-compose stop

# Pull latest code and rebuild
git pull origin main
docker-compose down -v
docker-compose up -d --build

# Backup database
docker-compose exec postgres pg_dump -U postgres postgres > backup.sql

# View database
docker-compose exec postgres psql -U postgres
```

---

## File Locations

On EC2 after deployment:

```
/home/ubuntu/inveee-app/
├── docker-compose.yml          ← Main orchestration file
├── .env                        ← Your secrets (customize!)
├── Invmgmt-master/             ← Frontend code
├── invmgmt.web/                ← Backend code
├── DEPLOYMENT.md               ← Full documentation
├── diagnose.sh                 ← Diagnostic tool
└── deploy.sh                   ← Re-deploy script
```

---

## Testing After Deployment

### 1. Frontend Loads
```bash
curl http://localhost/ | head -20
# Should return HTML (Angular app)
```

### 2. Backend API Responds
```bash
curl http://localhost:5000/health
# Should return: {"status":"healthy","database":"connected"...}
```

### 3. Database Connected
```bash
docker-compose exec postgres psql -U postgres -c "SELECT 1;"
# Should return: 1
```

### 4. Login Works
1. Open http://100.55.99.251 in browser
2. Enter credentials from `.env` (ADMIN_EMAIL, ADMIN_PASSWORD)
3. Should see dashboard

---

## Performance Monitoring

```bash
# Real-time resource usage
docker stats

# Disk usage
du -sh /home/ubuntu/inveee-app

# Memory available
free -h

# Logs with timestamps
docker-compose logs -f --timestamps
```

---

## Disaster Recovery

### If everything breaks:

```bash
# Full cleanup
docker-compose down -v
docker system prune -a --volumes

# Start fresh
git pull origin main
docker-compose up -d --build
```

### Restore from backup:

```bash
# With backup.sql file
docker-compose down -v
docker-compose up -d
# Wait for PostgreSQL to be healthy
sleep 30
docker-compose exec -T postgres psql -U postgres postgres < backup.sql
docker-compose restart backend
```

---

## Success Checklist ✅

- [ ] SSH to EC2 works
- [ ] Docker installed and running
- [ ] Repository cloned
- [ ] `.env` configured with your secrets
- [ ] `docker-compose up -d` executed
- [ ] All containers running: `docker-compose ps`
- [ ] Backend health check passes: `curl http://localhost:5000/health`
- [ ] Frontend loads: http://100.55.99.251/
- [ ] Can login with admin credentials
- [ ] API calls work (check browser DevTools)

---

## Support

**Stuck?** Run diagnostic tool:
```bash
chmod +x diagnose.sh
./diagnose.sh
```

**Need detailed help?** Read:
- `DEPLOYMENT.md` - Full troubleshooting guide
- `EC2_DEPLOYMENT_CHECKLIST.md` - Step-by-step walkthrough
- Logs: `docker-compose logs -f`

---

**Last Updated**: 2026-06-14

