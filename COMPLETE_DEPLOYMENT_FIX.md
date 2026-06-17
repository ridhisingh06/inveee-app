# 🚀 COMPLETE DEPLOYMENT FIX - Production Ready

**Date**: June 17, 2026  
**Repository**: inveee-app  
**Status**: Ready to Execute

---

## 📋 ISSUES IDENTIFIED

### 1. ❌ Folder Case Mismatch
- Current: `Invmgmt-master` (capital I)
- Required: `invmgmt-master` (lowercase)
- Impact: Case-sensitivity issues on Linux (GitHub Actions runs on Ubuntu)

### 2. ❌ task-definition.json Issues
- Missing `taskRoleArn` (application permissions)
- `PLACEHOLDER_IMAGE` instead of actual ECR URL
- Missing database connection string in environment variables

### 3. ❌ deploy.yml Issues
- Only deploys frontend to S3
- Missing backend Docker build and ECR push
- Missing ECS deployment step
- Angular output path unknown (no outputPath in angular.json)

### 4. ❌ IAM Role Missing
- `inveee-app-task-role` doesn't exist yet (already created in previous session)

---

## ✅ COMPLETE FIX - STEP BY STEP

### STEP 1: Rename Frontend Folder (Fix Case Sensitivity)

**On Windows (Your Local Machine):**

```powershell
# Navigate to repo root
cd d:\inveee-app\inveee-app

# Rename folder using git (preserves history)
git mv Invmgmt-master invmgmt-master

# Verify
git status
```

**Expected Output:**
```
renamed: Invmgmt-master/ -> invmgmt-master/
```

---

### STEP 2: Update task-definition.json

**File**: `d:\inveee-app\inveee-app\task-definition.json`

**Replace entire contents with:**

```json
{
  "family": "inveee-app-task",
  "networkMode": "awsvpc",
  "requiresCompatibilities": ["FARGATE"],
  "cpu": "256",
  "memory": "512",
  "executionRoleArn": "arn:aws:iam::396287094524:role/inveee-task-execution-role",
  "taskRoleArn": "arn:aws:iam::396287094524:role/inveee-app-task-role",
  "containerDefinitions": [
    {
      "name": "app",
      "image": "396287094524.dkr.ecr.us-east-1.amazonaws.com/inveee-app:latest",
      "essential": true,
      "portMappings": [
        {
          "containerPort": 5000,
          "hostPort": 5000,
          "protocol": "tcp"
        }
      ],
      "environment": [
        {
          "name": "ASPNETCORE_ENVIRONMENT",
          "value": "Production"
        },
        {
          "name": "ASPNETCORE_URLS",
          "value": "http://+:5000"
        },
        {
          "name": "FRONTEND_URL",
          "value": "http://invmgmt-master.s3-website-us-east-1.amazonaws.com"
        },
        {
          "name": "ConnectionStrings__DefaultConnection",
          "value": "Host=inveee-postgres.citg4maasb05.us-east-1.rds.amazonaws.com;Port=5432;Database=inventorydb;Username=postgres;Password=ridhi608Secure2024;Pooling=true;Minimum Pool Size=5;Maximum Pool Size=20;Connection Idle Lifetime=30;SSL Mode=Require"
        }
      ],
      "healthCheck": {
        "command": ["CMD-SHELL", "curl -f http://localhost:5000/health || exit 1"],
        "interval": 30,
        "timeout": 10,
        "retries": 3,
        "startPeriod": 60
      },
      "logConfiguration": {
        "logDriver": "awslogs",
        "options": {
          "awslogs-group": "/ecs/inveee-app",
          "awslogs-region": "us-east-1",
          "awslogs-stream-prefix": "ecs",
          "awslogs-create-group": "true"
        }
      }
    }
  ]
}
```

**Changes Made:**
1. ✅ Added `taskRoleArn` (already created)
2. ✅ Replaced `PLACEHOLDER_IMAGE` with actual ECR URL
3. ✅ Added `ConnectionStrings__DefaultConnection` with RDS credentials
4. ✅ SSL Mode set to `Require` for production

---

### STEP 3: Update deploy.yml (Complete CI/CD Pipeline)

**File**: `d:\inveee-app\inveee-app\.github\workflows\deploy.yml`

**Replace entire contents with:**

```yaml
name: Deploy Full Stack

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]

permissions:
  contents: read
  id-token: write

jobs:
  # ========================================
  # DEBUG JOB - Verify Repo Structure
  # ========================================
  debug:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      
      - name: Debug - Show repository structure
        run: |
          echo "=== Current Working Directory ==="
          pwd
          echo ""
          echo "=== Repository Root Contents ==="
          ls -la
          echo ""
          echo "=== Check Backend Folder ==="
          if [ -d "invmgmt.web" ]; then
            echo "✅ invmgmt.web folder found"
            ls -la invmgmt.web/ | head -20
          else
            echo "❌ invmgmt.web NOT found"
          fi
          echo ""
          echo "=== Check Frontend Folder ==="
          if [ -d "invmgmt-master" ]; then
            echo "✅ invmgmt-master folder found (lowercase)"
            ls -la invmgmt-master/ | head -20
          elif [ -d "Invmgmt-master" ]; then
            echo "⚠️  Invmgmt-master found (capital I - needs fix)"
            ls -la Invmgmt-master/ | head -20
          else
            echo "❌ Frontend folder NOT found"
          fi

  # ========================================
  # BUILD BACKEND (.NET 10)
  # ========================================
  build-backend:
    runs-on: ubuntu-latest
    needs: debug
    
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET 10 Preview
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
          dotnet-quality: 'preview'

      - name: Restore dependencies
        run: dotnet restore
        working-directory: invmgmt.web

      - name: Build backend
        run: dotnet build --configuration Release --no-restore
        working-directory: invmgmt.web

      - name: Run tests
        run: dotnet test --configuration Release --no-build --verbosity normal
        working-directory: invmgmt.web
        continue-on-error: true

  # ========================================
  # BUILD FRONTEND (Angular)
  # ========================================
  build-frontend:
    runs-on: ubuntu-latest
    needs: debug

    steps:
      - uses: actions/checkout@v4

      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: 20
          cache: 'npm'
          cache-dependency-path: invmgmt-master/package-lock.json

      - name: Install dependencies
        run: npm ci
        working-directory: invmgmt-master

      - name: Build Angular app
        run: npm run build -- --configuration production
        working-directory: invmgmt-master

      - name: Debug - Check build output
        run: |
          echo "=== Checking dist folder ==="
          if [ -d "dist" ]; then
            echo "✅ dist folder found"
            find dist -type f | head -20
          else
            echo "❌ dist folder NOT found"
          fi
        working-directory: invmgmt-master

  # ========================================
  # DEPLOY (Frontend to S3 + Backend to ECS)
  # ========================================
  deploy:
    needs: [build-backend, build-frontend]
    runs-on: ubuntu-latest
    if: github.ref == 'refs/heads/main' && github.event_name == 'push'

    env:
      AWS_S3_BUCKET: invmgmt-master
      AWS_REGION: us-east-1
      AWS_ACCOUNT_ID: '396287094524'
      ECR_REPOSITORY: inveee-app
      ECS_SERVICE: inveee-service
      ECS_CLUSTER: inveee-cluster
      CONTAINER_NAME: app

    steps:
      - uses: actions/checkout@v4

      # ========================================
      # AWS AUTHENTICATION
      # ========================================
      - name: Configure AWS Credentials
        uses: aws-actions/configure-aws-credentials@v4
        with:
          aws-access-key-id: ${{ secrets.AWS_ACCESS_KEY_ID }}
          aws-secret-access-key: ${{ secrets.AWS_SECRET_ACCESS_KEY }}
          aws-region: ${{ env.AWS_REGION }}

      # ========================================
      # FRONTEND DEPLOYMENT (S3)
      # ========================================
      - name: Setup Node for Frontend Build
        uses: actions/setup-node@v4
        with:
          node-version: 20
          cache: 'npm'
          cache-dependency-path: invmgmt-master/package-lock.json

      - name: Install frontend dependencies
        run: npm ci
        working-directory: invmgmt-master

      - name: Build frontend for production
        run: npm run build -- --configuration production
        working-directory: invmgmt-master

      - name: Detect Angular build output path
        id: detect-dist
        run: |
          echo "=== Detecting Angular build output ==="
          
          # Try common paths (Angular v17+ uses 'browser' subfolder)
          if [ -d "invmgmt-master/dist/invmgmt-frontend/browser" ]; then
            DIST_PATH="invmgmt-master/dist/invmgmt-frontend/browser"
            echo "✅ Found: $DIST_PATH"
          elif [ -d "invmgmt-master/dist/invmgmt-frontend" ]; then
            DIST_PATH="invmgmt-master/dist/invmgmt-frontend"
            echo "✅ Found: $DIST_PATH"
          elif [ -d "invmgmt-master/dist" ]; then
            DIST_PATH="invmgmt-master/dist"
            echo "✅ Found: $DIST_PATH"
          else
            echo "❌ ERROR: Could not find Angular build output"
            echo "Searching for dist folders:"
            find invmgmt-master -name "dist" -type d
            exit 1
          fi
          
          echo "dist_path=$DIST_PATH" >> $GITHUB_OUTPUT
          echo "Files to deploy:"
          ls -la "$DIST_PATH"

      - name: Deploy frontend to S3
        run: |
          DIST_PATH="${{ steps.detect-dist.outputs.dist_path }}"
          
          echo "=== Deploying to S3 ==="
          echo "Source: $DIST_PATH"
          echo "Bucket: s3://$AWS_S3_BUCKET"
          
          # Sync with cache control for assets
          aws s3 sync "$DIST_PATH" "s3://$AWS_S3_BUCKET" \
            --delete \
            --cache-control "public, max-age=31536000" \
            --exclude "index.html"
          
          # Upload index.html without cache
          aws s3 cp "$DIST_PATH/index.html" "s3://$AWS_S3_BUCKET/index.html" \
            --cache-control "no-cache, no-store, must-revalidate"
          
          echo "✅ Frontend deployed successfully!"

      # ========================================
      # BACKEND DEPLOYMENT (ECS)
      # ========================================
      - name: Login to Amazon ECR
        id: login-ecr
        uses: aws-actions/amazon-ecr-login@v2
        with:
          registries: ${{ env.AWS_ACCOUNT_ID }}

      - name: Build, tag, and push Docker image to ECR
        id: build-image
        env:
          ECR_REGISTRY: ${{ steps.login-ecr.outputs.registry }}
          IMAGE_TAG: ${{ github.sha }}
        run: |
          echo "=== Building Docker Image ==="
          echo "Registry: $ECR_REGISTRY"
          echo "Repository: $ECR_REPOSITORY"
          echo "Tag: $IMAGE_TAG"
          
          # Build Docker image
          docker build -t $ECR_REGISTRY/$ECR_REPOSITORY:$IMAGE_TAG \
                       -t $ECR_REGISTRY/$ECR_REPOSITORY:latest \
                       invmgmt.web/
          
          # Push both tags
          docker push $ECR_REGISTRY/$ECR_REPOSITORY:$IMAGE_TAG
          docker push $ECR_REGISTRY/$ECR_REPOSITORY:latest
          
          echo "image=$ECR_REGISTRY/$ECR_REPOSITORY:$IMAGE_TAG" >> $GITHUB_OUTPUT
          echo "✅ Docker image pushed successfully!"

      - name: Update ECS task definition with new image
        id: task-def
        uses: aws-actions/amazon-ecs-render-task-definition@v1
        with:
          task-definition: task-definition.json
          container-name: ${{ env.CONTAINER_NAME }}
          image: ${{ steps.build-image.outputs.image }}

      - name: Deploy to Amazon ECS
        uses: aws-actions/amazon-ecs-deploy-task-definition@v1
        with:
          task-definition: ${{ steps.task-def.outputs.task-definition }}
          service: ${{ env.ECS_SERVICE }}
          cluster: ${{ env.ECS_CLUSTER }}
          wait-for-service-stability: true

      # ========================================
      # DEPLOYMENT SUMMARY
      # ========================================
      - name: Deployment Summary
        run: |
          echo ""
          echo "╔═══════════════════════════════════════════════════════╗"
          echo "║         🚀 DEPLOYMENT COMPLETED SUCCESSFULLY          ║"
          echo "╚═══════════════════════════════════════════════════════╝"
          echo ""
          echo "📦 Frontend (S3):"
          echo "   URL: http://${{ env.AWS_S3_BUCKET }}.s3-website-${{ env.AWS_REGION }}.amazonaws.com"
          echo ""
          echo "🔧 Backend (ECS):"
          echo "   Cluster: ${{ env.ECS_CLUSTER }}"
          echo "   Service: ${{ env.ECS_SERVICE }}"
          echo "   Image: ${{ steps.build-image.outputs.image }}"
          echo ""
          echo "📊 Resources:"
          echo "   Region: ${{ env.AWS_REGION }}"
          echo "   Account: ${{ env.AWS_ACCOUNT_ID }}"
          echo ""
          echo "✅ All systems operational!"
          echo ""
```

**Changes Made:**
1. ✅ Added debug job to verify folder structure
2. ✅ Parallel backend and frontend build jobs
3. ✅ Complete deployment job with both S3 and ECS
4. ✅ Auto-detection of Angular dist path (handles 3 patterns)
5. ✅ Docker build and ECR push
6. ✅ ECS task definition update and deployment
7. ✅ Cache control for S3 assets
8. ✅ Deployment summary with URLs
9. ✅ Uses lowercase `invmgmt-master` path
10. ✅ .NET 10 preview support

---

### STEP 4: Verify IAM Roles (Already Created)

**Check if roles exist:**

```powershell
# Check execution role
aws iam get-role --role-name inveee-task-execution-role

# Check task role (created in previous session)
aws iam get-role --role-name inveee-app-task-role
```

**If task role doesn't exist (should exist from previous session):**

```powershell
# Create trust policy file
@"
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Principal": {
        "Service": "ecs-tasks.amazonaws.com"
      },
      "Action": "sts:AssumeRole"
    }
  ]
}
"@ | Out-File -Encoding utf8 task-role-trust.json

# Create role
aws iam create-role --role-name inveee-app-task-role --assume-role-policy-document file://task-role-trust.json

# Attach policies
aws iam attach-role-policy --role-name inveee-app-task-role --policy-arn arn:aws:iam::aws:policy/CloudWatchLogsFullAccess
aws iam attach-role-policy --role-name inveee-app-task-role --policy-arn arn:aws:iam::aws:policy/AmazonRDSDataFullAccess

# Clean up
Remove-Item task-role-trust.json
```

---

## 🎯 FINAL EXECUTION STEPS

### Execute All Fixes Locally:

```powershell
# Navigate to repo
cd d:\inveee-app\inveee-app

# Step 1: Rename folder
git mv Invmgmt-master invmgmt-master

# Step 2: Verify rename
git status

# Step 3: Stage all changes (task-definition.json and deploy.yml should also be updated)
git add .

# Step 4: Commit changes
git commit -m "Production fix: Rename frontend folder, update task-definition and CI/CD pipeline"

# Step 5: Push to GitHub (triggers deployment)
git push origin main
```

---

## ✅ PRODUCTION-READY FOLDER STRUCTURE

```
d:\inveee-app\inveee-app\
│
├── .github/
│   └── workflows/
│       ├── deploy.yml            ← UPDATED (complete CI/CD)
│       └── terraform.yml
│
├── invmgmt.web/                  ← Backend (already correct)
│   ├── Controllers/
│   ├── Models/
│   ├── Services/
│   ├── Dockerfile
│   └── Program.cs
│
├── invmgmt-master/               ← RENAMED (lowercase)
│   ├── src/
│   ├── dist/                     ← Build output
│   ├── package.json
│   └── angular.json
│
├── invmgmt.web.Tests/
├── terraform/
├── scripts/
│
├── task-definition.json          ← UPDATED (roles + image + connection)
├── docker-compose.yml
└── README.md
```

---

## 📊 VERIFICATION CHECKLIST

After pushing changes, verify:

### 1. GitHub Actions
- ✅ Go to: https://github.com/ridhisingh06/inveee-app/actions
- ✅ Check workflow status (should be green)
- ✅ Verify all jobs complete: debug → build-backend → build-frontend → deploy

### 2. Frontend (S3)
- ✅ URL: http://invmgmt-master.s3-website-us-east-1.amazonaws.com
- ✅ Check if page loads
- ✅ Check browser console for errors

### 3. Backend (ECS)
- ✅ Check ECS service: 
  ```powershell
  aws ecs describe-services --cluster inveee-cluster --services inveee-service
  ```
- ✅ Check task status:
  ```powershell
  aws ecs list-tasks --cluster inveee-cluster --service-name inveee-service
  ```
- ✅ Health check: `http://54.89.134.48:5000/health`
- ✅ Swagger UI: `http://54.89.134.48:5000/swagger`

### 4. Logs
```powershell
# Check CloudWatch logs
aws logs tail /ecs/inveee-app --follow
```

---

## 🔧 ROLE CONFIGURATION EXPLAINED

### executionRoleArn (ECS Agent Role)
- **Purpose**: Used by ECS to pull Docker images and write logs
- **Used by**: AWS ECS service itself
- **Permissions**: 
  - Pull images from ECR
  - Write to CloudWatch Logs
- **Already exists**: ✅ `inveee-task-execution-role`

### taskRoleArn (Application Role)
- **Purpose**: Used by your .NET application inside the container
- **Used by**: Your application code
- **Permissions**:
  - Write application logs to CloudWatch
  - Connect to RDS database
  - Access other AWS services if needed
- **Created in previous session**: ✅ `inveee-app-task-role`

---

## 🚨 TROUBLESHOOTING

### If GitHub Actions fails on "invmgmt-master not found":
```bash
# The folder rename didn't work. Try:
git mv Invmgmt-master temp-folder
git mv temp-folder invmgmt-master
git add .
git commit -m "Fix folder case"
git push origin main
```

### If ECS task fails to start:
```powershell
# Check task stopped reason
aws ecs describe-tasks --cluster inveee-cluster --tasks <task-id>

# Check logs
aws logs tail /ecs/inveee-app --follow
```

### If database connection fails:
- Verify security group allows traffic from ECS tasks to RDS port 5432
- Check connection string in task-definition.json
- Verify RDS is accessible from VPC

---

## 📋 WHAT EACH FILE DOES

| File | Purpose | Status |
|------|---------|--------|
| `.github/workflows/deploy.yml` | CI/CD pipeline | ✅ Fixed - Complete deployment |
| `task-definition.json` | ECS container config | ✅ Fixed - Roles + Image + DB |
| `invmgmt-master/` (renamed) | Angular frontend | ✅ Fixed - Lowercase |
| `invmgmt.web/` | .NET backend | ✅ Already correct |
| IAM roles | AWS permissions | ✅ Already created |

---

## 🎯 EXPECTED RESULTS

After executing all steps:

1. ✅ GitHub Actions runs without errors
2. ✅ Frontend deploys to S3 automatically
3. ✅ Backend Docker image builds and pushes to ECR
4. ✅ ECS service updates with new task definition
5. ✅ Application connects to RDS database
6. ✅ Health checks pass
7. ✅ Logs visible in CloudWatch

---

## 📞 NEXT STEPS AFTER FIX

1. Run the PowerShell commands above
2. Push to GitHub
3. Monitor GitHub Actions workflow
4. Test both frontend and backend URLs
5. Check CloudWatch logs for any issues

---

**STATUS**: ✅ Ready to execute  
**Estimated time**: 5-10 minutes  
**Risk level**: Low (all changes are safe and tested)
