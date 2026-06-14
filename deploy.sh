#!/bin/bash
# ===================================
# Full Stack Docker Deployment Script
# ===================================
# Usage: ./deploy.sh
# 
# Prerequisites on EC2:
# - docker
# - docker-compose
# - git
# - 100GB+ disk space

set -e  # Exit on any error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}=================================${NC}"
echo -e "${BLUE}InvMgmt Full Stack Deployment${NC}"
echo -e "${BLUE}=================================${NC}"

# Step 1: Check prerequisites
echo -e "\n${YELLOW}[1/7] Checking prerequisites...${NC}"
command -v docker >/dev/null 2>&1 || { echo -e "${RED}✗ Docker not installed${NC}"; exit 1; }
command -v docker-compose >/dev/null 2>&1 || { echo -e "${RED}✗ Docker Compose not installed${NC}"; exit 1; }
command -v git >/dev/null 2>&1 || { echo -e "${RED}✗ Git not installed${NC}"; exit 1; }
echo -e "${GREEN}✓ All prerequisites installed${NC}"

# Step 2: Clean up old deployment (if exists)
echo -e "\n${YELLOW}[2/7] Cleaning up existing deployment...${NC}"
REPO_PATH="/home/ubuntu/inveee-app"

if [ -d "$REPO_PATH" ]; then
    echo -e "${YELLOW}  Removing old repository...${NC}"
    cd "$REPO_PATH" || exit 1
    
    # Stop and remove containers
    docker-compose down -v 2>/dev/null || true
    
    # Go back to parent
    cd ..
    
    # Remove directory
    rm -rf "$REPO_PATH"
    echo -e "${GREEN}  ✓ Old deployment cleaned${NC}"
else
    echo -e "${YELLOW}  No existing deployment found${NC}"
fi

# Step 3: Clone/Pull latest repository
echo -e "\n${YELLOW}[3/7] Setting up repository...${NC}"
mkdir -p /home/ubuntu
cd /home/ubuntu

# Clone or update repository
if [ ! -d "$REPO_PATH" ]; then
    echo -e "${YELLOW}  Cloning repository...${NC}"
    git clone https://github.com/ridhisingh06/inveee-app.git
else
    echo -e "${YELLOW}  Updating repository...${NC}"
    cd "$REPO_PATH"
    git pull origin main
fi

cd "$REPO_PATH"
echo -e "${GREEN}  ✓ Repository ready at $REPO_PATH${NC}"

# Step 4: Setup environment variables
echo -e "\n${YELLOW}[4/7] Configuring environment...${NC}"

# Check if .env exists; if not, copy from .env.prod
if [ ! -f .env ]; then
    if [ -f .env.prod ]; then
        cp .env.prod .env
        echo -e "${YELLOW}  ⚠ Created .env from .env.prod${NC}"
        echo -e "${YELLOW}  Please edit .env with your production secrets!${NC}"
    else
        echo -e "${RED}  ✗ No .env or .env.prod found${NC}"
        exit 1
    fi
else
    echo -e "${GREEN}  ✓ Using existing .env${NC}"
fi

# Verify essential environment variables
if ! grep -q "JWT_KEY" .env; then
    echo -e "${RED}  ✗ JWT_KEY not configured in .env${NC}"
    exit 1
fi

if ! grep -q "POSTGRES_PASSWORD" .env; then
    echo -e "${RED}  ✗ POSTGRES_PASSWORD not configured in .env${NC}"
    exit 1
fi

echo -e "${GREEN}  ✓ Environment configured${NC}"

# Step 5: Build Docker images
echo -e "\n${YELLOW}[5/7] Building Docker images...${NC}"
echo -e "${YELLOW}  This may take 5-10 minutes...${NC}"

# Build backend
echo -e "${YELLOW}  Building backend...${NC}"
docker build -t invmgmt-backend:latest ./invmgmt.web -f ./invmgmt.web/Dockerfile
if [ $? -eq 0 ]; then
    echo -e "${GREEN}  ✓ Backend image built${NC}"
    echo -e "${YELLOW}  Cleaning up backend build dependencies to free disk space...${NC}"
    docker image rm mcr.microsoft.com/dotnet/sdk:10.0 2>/dev/null || true
    docker builder prune -af 2>/dev/null || true
else
    echo -e "${RED}  ✗ Backend build failed${NC}"
    exit 1
fi

# Build frontend
echo -e "${YELLOW}  Building frontend...${NC}"
docker build -t invmgmt-frontend:latest ./Invmgmt-master -f ./Invmgmt-master/Dockerfile
if [ $? -eq 0 ]; then
    echo -e "${GREEN}  ✓ Frontend image built${NC}"
else
    echo -e "${RED}  ✗ Frontend build failed${NC}"
    exit 1
fi

# Step 6: Start services with docker-compose
echo -e "\n${YELLOW}[6/7] Starting services...${NC}"
docker-compose up -d

if [ $? -eq 0 ]; then
    echo -e "${GREEN}  ✓ Services started${NC}"
else
    echo -e "${RED}  ✗ Failed to start services${NC}"
    exit 1
fi

# Step 7: Verify deployment
echo -e "\n${YELLOW}[7/7] Verifying deployment...${NC}"

# Wait for services to be healthy
echo -e "${YELLOW}  Waiting for services to be ready (60 seconds)...${NC}"
sleep 20

# Check PostgreSQL
if docker-compose ps postgres | grep -q "healthy"; then
    echo -e "${GREEN}  ✓ PostgreSQL is running${NC}"
else
    echo -e "${YELLOW}  ⚠ PostgreSQL status unknown (might still be initializing)${NC}"
fi

# Check Backend
if docker-compose ps backend | grep -q "Up"; then
    echo -e "${GREEN}  ✓ Backend is running${NC}"
    BACKEND_IP=$(docker-compose exec -T backend hostname -I | awk '{print $1}')
    echo -e "${BLUE}    API: http://localhost:5000${NC}"
else
    echo -e "${RED}  ✗ Backend is not running${NC}"
    docker-compose logs backend | tail -20
fi

# Check Frontend
if docker-compose ps frontend | grep -q "Up"; then
    echo -e "${GREEN}  ✓ Frontend is running${NC}"
    echo -e "${BLUE}    Web: http://localhost${NC}"
else
    echo -e "${RED}  ✗ Frontend is not running${NC}"
    docker-compose logs frontend | tail -20
fi

echo -e "\n${BLUE}=================================${NC}"
echo -e "${GREEN}✓ Deployment Complete!${NC}"
echo -e "${BLUE}=================================${NC}"

echo -e "\n${YELLOW}Service URLs:${NC}"
echo -e "  Frontend: http://100.55.99.251"
echo -e "  Backend:  http://100.55.99.251:5000"
echo -e "  API Docs: http://100.55.99.251:5000/swagger"

echo -e "\n${YELLOW}Useful Commands:${NC}"
echo -e "  View logs:        ${BLUE}docker-compose logs -f${NC}"
echo -e "  Backend logs:     ${BLUE}docker-compose logs -f backend${NC}"
echo -e "  Frontend logs:    ${BLUE}docker-compose logs -f frontend${NC}"
echo -e "  Database logs:    ${BLUE}docker-compose logs -f postgres${NC}"
echo -e "  Stop services:    ${BLUE}docker-compose down${NC}"
echo -e "  Remove volumes:   ${BLUE}docker-compose down -v${NC}"
echo -e "  Restart services: ${BLUE}docker-compose restart${NC}"

echo -e "\n${YELLOW}Next Steps:${NC}"
echo -e "  1. Update .env with production secrets"
echo -e "  2. Verify API health: curl http://100.55.99.251:5000/health"
echo -e "  3. Check backend logs: docker-compose logs backend"
echo -e "  4. Access frontend: http://100.55.99.251"
