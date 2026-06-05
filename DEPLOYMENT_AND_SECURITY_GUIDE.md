# Deployment & Security Guide - Inventory Management System

## Table of Contents
1. [Local Development Setup](#local-development-setup)
2. [Docker Deployment](#docker-deployment)
3. [Production Deployment](#production-deployment)
4. [Security Hardening](#security-hardening)
5. [SSL/TLS Configuration](#ssltls-configuration)
6. [Database Security](#database-security)
7. [API Security](#api-security)
8. [Monitoring & Alerts](#monitoring--alerts)
9. [Troubleshooting](#troubleshooting)

---

## Local Development Setup

### Prerequisites
- .NET 10.0 SDK
- Node.js 20+ & npm 11+
- PostgreSQL 15+
- Docker & Docker Compose (optional)

### Backend Setup

#### 1. Clone Repository
```bash
cd d:\inveeeR
```

#### 2. Restore Dependencies
```bash
cd invmgmt.web
dotnet restore
```

#### 3. Configure Database
Create `appsettings.Development.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=InvMgmtDb;Username=postgres;Password=your_password"
  },
  "Jwt": {
    "Key": "YOUR_SECRET_KEY_MIN_32_CHARS_REQUIRED_HERE",
    "Issuer": "invmgmt",
    "Audience": "invmgmt_user"
  },
  "Serilog": {
    "MinimumLevel": "Debug",
    "WriteTo": [
      {
        "Name": "Console"
      },
      {
        "Name": "File",
        "Args": {
          "path": "./Logs/log-.txt",
          "rollingInterval": "Day"
        }
      }
    ]
  }
}
```

#### 4. Create & Migrate Database
```bash
# Create database
dotnet ef database update

# Seed initial data (automatically on startup)
```

#### 5. Run Backend
```bash
dotnet run
# API available at http://localhost:5000
# Swagger at http://localhost:5000/swagger
```

### Frontend Setup

#### 1. Install Dependencies
```bash
cd Invmgmt-master
npm install
```

#### 2. Configure Environment
Create `src/environments/environment.ts`:
```typescript
export const environment = {
  production: false,
  apiUrl: 'http://localhost:5000/api',
  apiTimeout: 30000
};
```

#### 3. Run Development Server
```bash
ng serve
# Frontend available at http://localhost:4200
```

### Local Database Setup

#### Using PostgreSQL (Local Install)
```bash
# Create database
createdb -U postgres InvMgmtDb

# Connect and verify
psql -U postgres -d InvMgmtDb
```

#### Using Docker (Recommended)
```bash
docker run -d \
  --name invmgmt-db \
  -e POSTGRES_DB=InvMgmtDb \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_PASSWORD=ridhi@608 \
  -p 5432:5432 \
  postgres:15
```

---

## Docker Deployment

### Prerequisites
- Docker 24.0+
- Docker Compose 2.20+

### Build & Deploy

#### 1. Build Images
```bash
cd d:\inveeeR
docker-compose build
```

#### 2. Create .env File
```bash
# Create .env in project root
ASPNETCORE_ENVIRONMENT=Production
POSTGRES_USER=postgres
POSTGRES_PASSWORD=ridhi@608_CHANGE_IN_PROD
ADMIN_EMAIL=admin@gmail.com
ADMIN_PASSWORD=admin@123_CHANGE_IN_PROD
JWT_KEY=YOUR_SECRET_KEY_MIN_32_CHARS_REQUIRED_HERE_CHANGE_IN_PROD
```

#### 3. Start Services
```bash
docker-compose up -d
```

#### 4. Verify Services
```bash
# Check status
docker-compose ps

# View logs
docker-compose logs -f

# Test health
curl http://localhost:5001/health
curl http://localhost:4200
```

#### 5. Stop Services
```bash
docker-compose down
```

### Docker Compose Services

| Service | Port | Purpose |
|---------|------|---------|
| frontend | 4200 | Angular UI |
| backend | 5001 | ASP.NET API |
| db | 5433 | PostgreSQL Database |
| seq | 8082 | Structured Logging |

### Docker Cleanup
```bash
# Remove stopped containers
docker-compose down -v

# Remove unused images
docker image prune -a

# Remove unused volumes
docker volume prune
```

---

## Production Deployment

### Architecture Overview
```
Internet
   ↓
Load Balancer (Nginx/HAProxy)
   ├─ Frontend (Nginx serving Angular)
   └─ Backend (ASP.NET Core behind reverse proxy)
         ↓
   PostgreSQL Database
         ↓
   Backup Storage
```

### Step 1: Environment Configuration

#### Production .env
```bash
# Environment
ASPNETCORE_ENVIRONMENT=Production
NODE_ENV=production

# Database
POSTGRES_USER=invmgmt_user
POSTGRES_PASSWORD=$(openssl rand -base64 32)
DB_HOST=db.internal.company.com
DB_PORT=5432
DB_NAME=InvMgmtDb_Prod

# Security
JWT_KEY=$(openssl rand -base64 64)
ADMIN_EMAIL=admin@company.com
ADMIN_PASSWORD=$(openssl rand -base64 32)

# SSL/TLS
SSL_CERTIFICATE_PATH=/etc/ssl/certs/invmgmt.crt
SSL_KEY_PATH=/etc/ssl/private/invmgmt.key

# Logging
SERILOG_LEVEL=Information
SEQ_SERVER=http://seq:5341

# CORS
CORS_ORIGINS=https://invmgmt.company.com

# Rate Limiting
RATE_LIMIT_REQUESTS=1000
RATE_LIMIT_WINDOW_MINUTES=15
```

### Step 2: Build Production Images

#### Backend Dockerfile
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS builder
WORKDIR /build
COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=builder /app .
EXPOSE 5000
HEALTHCHECK --interval=15s --timeout=10s --retries=5 \
  CMD curl -f http://localhost:5000/health || exit 1
ENTRYPOINT ["dotnet", "invmgmt.web.dll"]
```

#### Frontend Dockerfile
```dockerfile
FROM node:20-alpine AS builder
WORKDIR /build
COPY . .
RUN npm ci
RUN npm run build

FROM nginx:alpine
COPY --from=builder /build/dist/invmgmt-frontend /usr/share/nginx/html
COPY nginx.conf /etc/nginx/nginx.conf
EXPOSE 80
HEALTHCHECK --interval=15s --timeout=5s --retries=3 \
  CMD wget -q -O- http://localhost/health || exit 1
```

### Step 3: Kubernetes Deployment (Optional)

#### Backend Deployment
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: invmgmt-backend
  namespace: invmgmt
spec:
  replicas: 3
  selector:
    matchLabels:
      app: invmgmt-backend
  template:
    metadata:
      labels:
        app: invmgmt-backend
    spec:
      containers:
      - name: backend
        image: invmgmt/backend:latest
        ports:
        - containerPort: 5000
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        - name: ConnectionStrings__DefaultConnection
          valueFrom:
            secretKeyRef:
              name: db-credentials
              key: connection-string
        resources:
          requests:
            memory: "512Mi"
            cpu: "250m"
          limits:
            memory: "1Gi"
            cpu: "500m"
        livenessProbe:
          httpGet:
            path: /health
            port: 5000
          initialDelaySeconds: 30
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /health
            port: 5000
          initialDelaySeconds: 10
          periodSeconds: 5
---
apiVersion: v1
kind: Service
metadata:
  name: invmgmt-backend-service
  namespace: invmgmt
spec:
  selector:
    app: invmgmt-backend
  type: LoadBalancer
  ports:
  - protocol: TCP
    port: 80
    targetPort: 5000
```

### Step 4: Nginx Reverse Proxy

```nginx
# /etc/nginx/sites-available/invmgmt.conf

upstream backend {
    least_conn;
    server backend:5000 max_fails=3 fail_timeout=30s;
    server backend:5000 max_fails=3 fail_timeout=30s;
    server backend:5000 max_fails=3 fail_timeout=30s;
    keepalive 32;
}

server {
    listen 80;
    server_name invmgmt.company.com;
    
    # Redirect HTTP to HTTPS
    return 301 https://$server_name$request_uri;
}

server {
    listen 443 ssl http2;
    server_name invmgmt.company.com;
    
    # SSL Configuration
    ssl_certificate /etc/ssl/certs/invmgmt.crt;
    ssl_certificate_key /etc/ssl/private/invmgmt.key;
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers HIGH:!aNULL:!MD5;
    ssl_prefer_server_ciphers on;
    
    # Security Headers
    add_header Strict-Transport-Security "max-age=31536000; includeSubDomains" always;
    add_header X-Frame-Options "DENY" always;
    add_header X-Content-Type-Options "nosniff" always;
    add_header X-XSS-Protection "1; mode=block" always;
    add_header Referrer-Policy "strict-origin-when-cross-origin" always;
    
    # Compression
    gzip on;
    gzip_types text/plain text/css text/javascript application/json;
    gzip_min_length 1000;
    
    # Frontend
    location / {
        root /usr/share/nginx/html;
        try_files $uri $uri/ /index.html;
        expires 1h;
    }
    
    # API Proxy
    location /api/ {
        proxy_pass http://backend/api/;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        
        # Timeouts
        proxy_connect_timeout 60s;
        proxy_send_timeout 60s;
        proxy_read_timeout 60s;
    }
    
    # Health Check
    location /health {
        proxy_pass http://backend/health;
        access_log off;
    }
    
    # Swagger (restricted to internal network)
    location /swagger {
        allow 10.0.0.0/8;
        deny all;
        proxy_pass http://backend/swagger;
    }
}
```

---

## Security Hardening

### 1. Environment Variables

**Never commit secrets:**
```bash
# .gitignore
.env
appsettings.Production.json
*.key
*.pem
```

**Use secret management:**
```bash
# Docker Secrets
docker secret create jwt_key ./jwt.key
docker secret create db_password ./db.pass

# Kubernetes Secrets
kubectl create secret generic db-credentials \
  --from-literal=connection-string="Host=db;..."
```

### 2. Authentication Hardening

#### Backend (Program.cs)
```csharp
// Strong JWT configuration
var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY");
if (string.IsNullOrEmpty(jwtKey) || jwtKey.Length < 32)
    throw new InvalidOperationException("JWT_KEY must be at least 32 characters");

// Password policy
builder.Services.AddScoped<IPasswordValidator<User>>(provider =>
    new PasswordValidator<User>
    {
        RequiredLength = 12,
        RequireNonAlphanumeric = true,
        RequireDigit = true,
        RequireUppercase = true,
        RequireLowercase = true
    }
);
```

#### CORS Policy
```csharp
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var allowedOrigins = Environment.GetEnvironmentVariable("CORS_ORIGINS")
            ?.Split(",") ?? new[] { "http://localhost:4200" };
        
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});
```

### 3. Rate Limiting

#### Add NuGet Package
```bash
dotnet add package AspNetCoreRateLimit
```

#### Configure Rate Limiting
```csharp
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();

app.UseIpRateLimiting();
```

#### appsettings.json
```json
{
  "IpRateLimiting": {
    "EnableEndpointRateLimiting": true,
    "StackBlockedRequests": false,
    "RealIpHeader": "X-Real-IP",
    "ClientIdHeader": "X-ClientId",
    "HttpStatusCode": 429,
    "IpWhitelist": [],
    "EndpointWhitelist": [],
    "ClientWhitelist": [],
    "GeneralRules": [
      {
        "Endpoint": "*",
        "Period": "15m",
        "Limit": 1000
      },
      {
        "Endpoint": "*/auth/login",
        "Period": "15m",
        "Limit": 5
      }
    ]
  }
}
```

### 4. SQL Injection Prevention

**Use parameterized queries (EF Core):**
```csharp
// Good ✅
var users = await _context.Users
    .Where(u => u.Email == email)
    .ToListAsync();

// Avoid ❌
var users = await _context.Users
    .FromSqlInterpolated($"SELECT * FROM Users WHERE Email = {email}")
    .ToListAsync();
```

### 5. Input Validation

```csharp
[Authorize(Roles = "USER")]
[HttpPost]
public async Task<IActionResult> CreateRequest([FromBody] CreateRequestFromCartDto dto)
{
    // Validate input
    if (dto?.Items == null || dto.Items.Count == 0)
        return BadRequest("Items list is required");
    
    foreach (var item in dto.Items)
    {
        if (item.Quantity <= 0 || item.Quantity > 1000)
            return BadRequest("Invalid quantity");
    }
    
    // ... rest of logic
}
```

### 6. Output Encoding

**Frontend XSS Prevention:**
```typescript
// Angular sanitizes by default
import { DomSanitizer } from '@angular/platform-browser';

constructor(private sanitizer: DomSanitizer) {}

getSafeHtml(html: string) {
    return this.sanitizer.bypassSecurityTrustHtml(html);
}
```

---

## SSL/TLS Configuration

### Self-Signed Certificate (Development)

```bash
# Generate private key
openssl genrsa -out invmgmt.key 2048

# Generate certificate
openssl req -new -x509 -key invmgmt.key -out invmgmt.crt -days 365 \
  -subj "/C=IN/ST=State/L=City/O=Company/CN=invmgmt.local"

# Convert to PFX (for IIS/Windows)
openssl pkcs12 -export -out invmgmt.pfx -inkey invmgmt.key -in invmgmt.crt
```

### Production Certificate (Let's Encrypt)

```bash
# Install Certbot
sudo apt-get install certbot python3-certbot-nginx

# Obtain certificate
sudo certbot certonly --nginx -d invmgmt.company.com

# Auto-renewal
sudo systemctl enable certbot.timer
sudo systemctl start certbot.timer
```

### HTTPS in ASP.NET Core

```csharp
// Force HTTPS
app.UseHttpsRedirection();

// HSTS
app.UseHsts();

// Add security headers
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("Strict-Transport-Security", "max-age=31536000");
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    await next();
});
```

---

## Database Security

### 1. User Privileges

```sql
-- Create application user
CREATE USER invmgmt_app WITH PASSWORD 'secure_password_here';

-- Grant limited privileges
GRANT CONNECT ON DATABASE InvMgmtDb TO invmgmt_app;
GRANT USAGE ON SCHEMA public TO invmgmt_app;
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO invmgmt_app;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO invmgmt_app;

-- Set default privileges
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO invmgmt_app;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT USAGE, SELECT ON SEQUENCES TO invmgmt_app;
```

### 2. Connection String Security

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=db.internal;Port=5432;Database=InvMgmtDb;Username=invmgmt_app;Password=${DB_PASSWORD};SslMode=Require;TrustServerCertificate=false"
  }
}
```

### 3. Encryption

**Database-level encryption:**
```bash
# PostgreSQL - Transparent Data Encryption (in enterprise versions)
# or use column-level encryption:

UPDATE "Users" 
SET "PasswordHash" = pgp_sym_encrypt("PasswordHash", '${ENCRYPTION_KEY}')
WHERE "Id" > 0;
```

### 4. Backup Encryption

```bash
# Backup with encryption
pg_dump InvMgmtDb | openssl enc -aes-256-cbc -salt -out backup.sql.enc

# Restore from encrypted backup
openssl enc -aes-256-cbc -d -in backup.sql.enc | psql -d InvMgmtDb
```

---

## API Security

### 1. Authentication Headers

```typescript
// Frontend HTTP Interceptor
import { Injectable } from '@angular/core';
import { HttpInterceptor, HttpRequest, HttpHandler, HttpEvent } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    const token = localStorage.getItem('token');
    
    if (token) {
      req = req.clone({
        setHeaders: {
          Authorization: `Bearer ${token}`
        }
      });
    }
    
    return next.handle(req);
  }
}
```

### 2. CSRF Protection

```csharp
// Add CSRF protection
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
});

app.UseAntiforgery();
```

### 3. Request Validation

```csharp
[ApiController]
public class BaseApiController : ControllerBase
{
    protected IActionResult ValidateRequest<T>(T data, params string[] requiredFields) where T : class
    {
        if (data == null)
            return BadRequest("Request body is required");
        
        var properties = typeof(T).GetProperties();
        foreach (var field in requiredFields)
        {
            var prop = properties.FirstOrDefault(p => p.Name == field);
            var value = prop?.GetValue(data);
            
            if (value == null || (value is string str && string.IsNullOrWhiteSpace(str)))
                return BadRequest($"{field} is required");
        }
        
        return Ok();
    }
}
```

---

## Monitoring & Alerts

### 1. Application Monitoring

#### New Relic Setup
```csharp
// appsettings.json
{
  "NewRelic": {
    "AgentEnabled": true,
    "AppName": "InvMgmt",
    "LicenseKey": "${NEW_RELIC_LICENSE_KEY}"
  }
}
```

#### Prometheus Metrics
```bash
dotnet add package prometheus-net.AspNetCore
```

```csharp
app.UseMetricServer();
app.UseHttpMetrics();
```

### 2. Log Aggregation

#### Serilog Configuration
```csharp
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)
    .WriteTo.Seq("http://seq:5341")
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "InvMgmt")
    .Enrich.WithEnvironmentUserName()
    .CreateLogger();
```

### 3. Alert Rules

#### Database Connection Failure
```
Alert: Database connection failed
Condition: /health endpoint returns 503
Action: Notify DevOps team
```

#### High Error Rate
```
Alert: High API error rate
Condition: >5% 5xx errors in 5 minutes
Action: Page on-call engineer
```

#### Unauthorized Access Attempts
```
Alert: Multiple failed login attempts
Condition: >10 failed logins from same IP in 15 minutes
Action: Block IP, notify security team
```

---

## Troubleshooting

### Common Issues

#### 1. Database Connection Timeout
**Error:** `Npgsql.NpgsqlException: Failed to establish a connection`

**Solution:**
```bash
# Check database is running
docker ps | grep db

# Test connection
psql -h localhost -U postgres -d InvMgmtDb

# Check connection string in appsettings
# Verify password and credentials
```

#### 2. JWT Token Expired
**Error:** `401 Unauthorized: Token expired`

**Solution:**
```typescript
// Implement token refresh
if (error.status === 401) {
  // Call refresh token endpoint
  this.authService.refreshToken().subscribe(...);
}
```

#### 3. CORS Error
**Error:** `Access to XMLHttpRequest blocked by CORS policy`

**Solution:**
```csharp
// Update CORS_ORIGINS environment variable
CORS_ORIGINS=https://invmgmt.company.com,https://staging.invmgmt.company.com
```

#### 4. SSL Certificate Error
**Error:** `The SSL connection could not be established`

**Solution:**
```bash
# Verify certificate
openssl x509 -in invmgmt.crt -text -noout

# Check certificate expiration
openssl x509 -enddate -noout -in invmgmt.crt

# Renew certificate
sudo certbot renew
```

#### 5. High Memory Usage
**Symptom:** Application crashes with OOM error

**Solution:**
```dockerfile
# Increase Docker memory limit
docker run -m 2g invmgmt-backend

# Or in docker-compose.yml
services:
  backend:
    mem_limit: 2g
```

### Debugging

#### Enable Debug Mode
```csharp
// appsettings.Development.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft": "Debug"
    }
  }
}
```

#### View Logs
```bash
# Docker logs
docker logs -f invmgmt_backend_1

# File logs
tail -f invmgmt.web/Logs/log-*.txt

# Seq dashboard
# http://localhost:8082
```

#### Database Debugging
```sql
-- Check active connections
SELECT pid, usename, application_name, state FROM pg_stat_activity;

-- Kill hanging query
SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE query LIKE '%...%';

-- Check locks
SELECT * FROM pg_locks WHERE NOT granted;
```

---

## Compliance Checklist

- [ ] GDPR compliance (data deletion, export)
- [ ] PCI-DSS compliance (if handling payments)
- [ ] SOC 2 audit readiness
- [ ] Data retention policy implemented
- [ ] Encryption in transit (HTTPS)
- [ ] Encryption at rest
- [ ] Access control (RBAC)
- [ ] Audit logging enabled
- [ ] Backup & recovery tested
- [ ] Incident response plan
- [ ] Security patch management
- [ ] Vulnerability assessment completed
- [ ] Penetration testing passed
- [ ] Employee security training
- [ ] Incident response team trained

---

## Useful Resources

- [OWASP Top 10](https://owasp.org/Top10/)
- [CWE/SANS Top 25](https://cwe.mitre.org/top25/)
- [Microsoft Security Best Practices](https://learn.microsoft.com/en-us/dotnet/standard/security/)
- [PostgreSQL Security](https://www.postgresql.org/docs/current/sql-syntax.html)
- [Let's Encrypt](https://letsencrypt.org/)

---

## Support & Escalation

**Security Issues:** security@company.com

**DevOps Support:** devops@company.com

**Emergency:** +1-XXX-XXX-XXXX (24/7 hotline)

---

Last Updated: **June 5, 2026**
Version: 1.0
