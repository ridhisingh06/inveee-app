# EC2 Deployment Checklist

## Step 1: EC2 Instance Setup

### Prerequisites on EC2
```bash
# SSH into EC2
ssh -i /path/to/key.pem ubuntu@100.55.99.251

# Install Docker and Docker Compose
sudo apt update
sudo apt install -y docker.io docker-compose git

# Add ubuntu user to docker group (no sudo needed)
sudo usermod -aG docker ubuntu
newgrp docker

# Verify installation
docker --version
docker-compose --version
```

### Security Group Configuration (AWS Console)
- ✅ Inbound SSH: Port 22 from your IP (already done)
- ✅ Inbound HTTP: Port 80 from 0.0.0.0/0 (for frontend)
- ✅ Inbound HTTPS: Port 443 from 0.0.0.0/0 (for SSL, optional)
- ✅ Inbound API: Port 5000 from 0.0.0.0/0 (for backend API)
- ✅ Database: Port 5432 should NOT be exposed to 0.0.0.0/0

---

## Step 2: Deploy Application

### Option A: Automated Deployment (Recommended)

**From your local machine:**

```powershell
# Windows (PowerShell)
cd d:\inveee-app
.\deploy-remote.ps1 -InstanceIP 100.55.99.251 -KeyPath "C:\Users\Singh\Downloads\inveeemgmt.pem"
```

```bash
# Mac/Linux
cd ~/inveee-app
ssh -i ~/inveeemgmt.pem ubuntu@100.55.99.251 'bash -s' < deploy.sh
```

### Option B: Manual Deployment (Step-by-step)

**On EC2 instance:**

```bash
# 1. Clone repository
git clone https://github.com/ridhisingh06/inveee-app.git
cd inveee-app

# 2. Setup environment
cp .env.prod .env
nano .env  # Edit with production values

# Required changes in .env:
# - POSTGRES_PASSWORD=YourSecurePassword123!
# - JWT_KEY=$(openssl rand -base64 32)
# - ADMIN_PASSWORD=YourAdminPassword123!
# - ADMIN_EMAIL=admin@yourdomain.com

# 3. Build and start services
docker-compose up -d --build

# 4. Wait for services to be healthy (2-3 minutes)
docker-compose ps

# 5. Check logs
docker-compose logs -f
```

---

## Step 3: Verify Deployment

### Health Checks

```bash
# Check all services running
docker-compose ps
# Expected output:
#   invmgmt-postgres   ✓ running
#   invmgmt-backend    ✓ running
#   invmgmt-frontend   ✓ running

# Test Backend API
curl http://localhost:5000/health
# Expected: {"status":"healthy","timestamp":"..."}

# Test Frontend
curl http://localhost/
# Expected: HTML response (Angular app)
```

### From Your Local Browser

```
Frontend:     http://100.55.99.251/
Backend API:  http://100.55.99.251:5000
API Docs:     http://100.55.99.251:5000/swagger
Health Check: http://100.55.99.251:5000/health
```

### Login Verification

1. Open http://100.55.99.251 in browser
2. Login with admin account:
   - Email: admin@example.com (from .env)
   - Password: AdminPassword123! (from .env)
3. Verify dashboard loads without API errors

---

## Step 4: Troubleshooting

### Issue: Cannot connect to instance
```bash
# Check security group
aws ec2 describe-security-groups --group-ids sg-xxxxx

# Test SSH connection
ssh -i key.pem ubuntu@100.55.99.251 "echo OK"

# Check instance is running
aws ec2 describe-instances --instance-ids i-xxxxx
```

### Issue: Docker commands fail
```bash
# Check docker daemon
sudo systemctl status docker

# Restart docker
sudo systemctl restart docker

# Verify group membership
id ubuntu  # Should show docker group
```

### Issue: Build fails or containers exit
```bash
# View detailed logs
docker-compose logs backend
docker-compose logs frontend
docker-compose logs postgres

# Check resources
docker stats

# Rebuild without cache
docker-compose down -v
docker system prune -a
docker-compose up -d --build
```

### Issue: API connection errors
```bash
# Test backend from inside frontend container
docker-compose exec frontend curl http://backend:5000/health

# Check Nginx proxy config
docker exec invmgmt-frontend cat /etc/nginx/conf.d/default.conf

# Test backend directly
docker-compose exec backend curl http://localhost:5000/health
```

---

## Step 5: Post-Deployment Tasks

### ✅ Security Hardening

```bash
# 1. Change admin password after first login
# (Do this from web UI)

# 2. Generate strong JWT key
openssl rand -base64 32
# Update .env JWT_KEY and restart:
# nano .env
# docker-compose restart backend

# 3. Enable HTTPS (Let's Encrypt + Certbot)
sudo apt install -y certbot python3-certbot-nginx
sudo certbot certonly --standalone -d your-domain.com
# Update Nginx config with SSL certificates

# 4. Enable firewall
sudo ufw enable
sudo ufw allow 22/tcp
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp
```

### ✅ Database Backup

```bash
# Backup database
docker-compose exec postgres pg_dump -U postgres postgres > backup.sql

# Upload to S3 (optional)
aws s3 cp backup.sql s3://your-bucket/backups/
```

### ✅ Monitoring Setup

```bash
# View live logs
docker-compose logs -f

# Setup log rotation
sudo vi /etc/docker/daemon.json
# Add:
# {
#   "log-driver": "json-file",
#   "log-opts": {
#     "max-size": "10m",
#     "max-file": "3"
#   }
# }

# Restart docker
sudo systemctl restart docker
```

---

## Step 6: Daily Operations

### Common Commands

```bash
# View service status
docker-compose ps

# View all logs
docker-compose logs -f

# View backend logs only
docker-compose logs -f backend

# Restart a service
docker-compose restart backend

# Restart all services
docker-compose restart

# Stop services (database persists)
docker-compose stop

# Stop and remove everything
docker-compose down

# Remove everything including data
docker-compose down -v

# Pull latest code and redeploy
git pull origin main
docker-compose down -v
docker-compose up -d --build
```

### Monitor Resource Usage
```bash
# CPU, Memory, Network
docker stats

# Disk usage
du -sh *

# Check available space
df -h
```

---

## Step 7: Environment Variables Reference

All variables used by services:

```env
# PostgreSQL
POSTGRES_USER=postgres
POSTGRES_PASSWORD=your-secure-password       # CHANGE THIS!
POSTGRES_DB=postgres

# Backend (.NET)
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:5000
ConnectionStrings__DefaultConnection=Host=postgres;...
JWT_KEY=your-jwt-key-min-32-chars           # CHANGE THIS!
JWT_ISSUER=invmgmt-api
JWT_AUDIENCE=invmgmt-frontend
ADMIN_EMAIL=admin@example.com
ADMIN_PASSWORD=your-admin-password          # CHANGE THIS!
```

---

## Success Indicators ✅

- [x] All containers running: `docker-compose ps` shows all "Up"
- [x] Backend health check passes: `curl http://localhost:5000/health`
- [x] Frontend loads: `curl http://localhost/` returns HTML
- [x] API responses work: Browser DevTools Network tab shows API calls
- [x] Login works: Can authenticate with admin credentials
- [x] Database connected: Backend logs show no connection errors

---

## Support & Next Steps

1. **Monitor first 24 hours**: `docker-compose logs -f`
2. **Test all workflows**: Create items, approve requests, export reports
3. **Setup SSL certificate**: Use Let's Encrypt for HTTPS
4. **Configure backups**: Daily database exports to S3
5. **Document customizations**: Keep track of any changes to .env or configs

