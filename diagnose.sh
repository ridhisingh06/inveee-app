#!/bin/bash
# ===================================
# InvMgmt Deployment Diagnostic Tool
# ===================================
# Usage: ./diagnose.sh

set -e

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

PASS_COUNT=0
FAIL_COUNT=0
WARN_COUNT=0

# Helper functions
pass() {
    echo -e "${GREEN}✓ $1${NC}"
    ((PASS_COUNT++))
}

fail() {
    echo -e "${RED}✗ $1${NC}"
    ((FAIL_COUNT++))
}

warn() {
    echo -e "${YELLOW}⚠ $1${NC}"
    ((WARN_COUNT++))
}

header() {
    echo -e "\n${BLUE}==== $1 ====${NC}"
}

# Check if running from correct directory
if [ ! -f "docker-compose.yml" ]; then
    echo -e "${RED}✗ docker-compose.yml not found${NC}"
    echo "Please run this script from the project root directory"
    exit 1
fi

echo -e "${BLUE}================================${NC}"
echo -e "${BLUE}InvMgmt Deployment Diagnostics${NC}"
echo -e "${BLUE}================================${NC}"

# ===================================
# 1. System Check
# ===================================
header "System Requirements"

# Docker
if command -v docker &> /dev/null; then
    DOCKER_VERSION=$(docker --version)
    pass "Docker installed: $DOCKER_VERSION"
else
    fail "Docker not installed"
    exit 1
fi

# Docker Compose
if command -v docker-compose &> /dev/null; then
    DC_VERSION=$(docker-compose --version)
    pass "Docker Compose installed: $DC_VERSION"
else
    fail "Docker Compose not installed"
    exit 1
fi

# Git
if command -v git &> /dev/null; then
    pass "Git installed"
else
    warn "Git not installed (needed for deployments)"
fi

# Disk space
DISK_USAGE=$(df -h / | awk 'NR==2 {print $5}' | sed 's/%//')
if [ "$DISK_USAGE" -lt 80 ]; then
    pass "Disk usage: $DISK_USAGE%"
else
    warn "Low disk space: $DISK_USAGE%"
fi

# Memory
MEMORY=$(free -h | awk 'NR==2 {print $2}')
pass "Available memory: $MEMORY"

# ===================================
# 2. Docker Environment
# ===================================
header "Docker Environment"

# Docker daemon
if docker ps &> /dev/null; then
    pass "Docker daemon running"
else
    fail "Docker daemon not responding"
    exit 1
fi

# Docker user group
if id -nG ubuntu | grep -qw docker; then
    pass "ubuntu user in docker group"
else
    warn "ubuntu user not in docker group (may need: sudo usermod -aG docker ubuntu)"
fi

# Images
BACKEND_IMG=$(docker images | grep -c invmgmt-backend || echo "0")
FRONTEND_IMG=$(docker images | grep -c invmgmt-frontend || echo "0")

if [ "$BACKEND_IMG" -gt 0 ]; then
    pass "Backend image exists"
else
    warn "Backend image not found (will be built on docker-compose up)"
fi

if [ "$FRONTEND_IMG" -gt 0 ]; then
    pass "Frontend image exists"
else
    warn "Frontend image not found (will be built on docker-compose up)"
fi

# ===================================
# 3. Container Status
# ===================================
header "Container Status"

# Check if containers exist
POSTGRES_COUNT=$(docker ps -a | grep -c invmgmt-postgres || echo "0")
BACKEND_COUNT=$(docker ps -a | grep -c invmgmt-backend || echo "0")
FRONTEND_COUNT=$(docker ps -a | grep -c invmgmt-frontend || echo "0")

if [ "$POSTGRES_COUNT" -gt 0 ]; then
    if docker ps | grep -q invmgmt-postgres; then
        pass "PostgreSQL container running"
    else
        fail "PostgreSQL container not running"
    fi
else
    warn "PostgreSQL container doesn't exist (needs docker-compose up)"
fi

if [ "$BACKEND_COUNT" -gt 0 ]; then
    if docker ps | grep -q invmgmt-backend; then
        pass "Backend container running"
    else
        fail "Backend container not running"
    fi
else
    warn "Backend container doesn't exist (needs docker-compose up)"
fi

if [ "$FRONTEND_COUNT" -gt 0 ]; then
    if docker ps | grep -q invmgmt-frontend; then
        pass "Frontend container running"
    else
        fail "Frontend container not running"
    fi
else
    warn "Frontend container doesn't exist (needs docker-compose up)"
fi

# ===================================
# 4. Network Connectivity
# ===================================
header "Network Connectivity"

# Network
NETWORK_COUNT=$(docker network ls | grep -c invmgmt-network || echo "0")
if [ "$NETWORK_COUNT" -gt 0 ]; then
    pass "Docker network exists"
else
    warn "Docker network doesn't exist (will be created)"
fi

# Port availability
if netstat -tuln 2>/dev/null | grep -q :5432; then
    pass "Port 5432 available (or in use)"
else
    warn "Port 5432 not detected (might be OK if not listening)"
fi

if netstat -tuln 2>/dev/null | grep -q :5000; then
    pass "Port 5000 available (or in use)"
else
    warn "Port 5000 not detected"
fi

if netstat -tuln 2>/dev/null | grep -q :80; then
    pass "Port 80 available (or in use)"
else
    warn "Port 80 not detected"
fi

# ===================================
# 5. Environment Configuration
# ===================================
header "Environment Configuration"

# .env file
if [ -f ".env" ]; then
    pass ".env file exists"
    
    # Check critical variables
    if grep -q "POSTGRES_PASSWORD" .env; then
        pass "POSTGRES_PASSWORD configured"
    else
        fail "POSTGRES_PASSWORD not in .env"
    fi
    
    if grep -q "JWT_KEY" .env; then
        JWT_LEN=$(grep "JWT_KEY" .env | cut -d'=' -f2 | wc -c)
        if [ "$JWT_LEN" -gt 20 ]; then
            pass "JWT_KEY configured (length: $JWT_LEN chars)"
        else
            warn "JWT_KEY too short (should be 32+ chars)"
        fi
    else
        fail "JWT_KEY not in .env"
    fi
    
    if grep -q "ADMIN_PASSWORD" .env; then
        pass "ADMIN_PASSWORD configured"
    else
        fail "ADMIN_PASSWORD not in .env"
    fi
else
    fail ".env file not found (copy from .env.prod)"
fi

# docker-compose.yml
if [ -f "docker-compose.yml" ]; then
    pass "docker-compose.yml exists"
    
    if grep -q "invmgmt-postgres\|postgres:" docker-compose.yml; then
        pass "PostgreSQL service defined"
    else
        fail "PostgreSQL service not defined"
    fi
    
    if grep -q "invmgmt-backend\|backend:" docker-compose.yml; then
        pass "Backend service defined"
    else
        fail "Backend service not defined"
    fi
    
    if grep -q "invmgmt-frontend\|frontend:" docker-compose.yml; then
        pass "Frontend service defined"
    else
        fail "Frontend service not defined"
    fi
else
    fail "docker-compose.yml not found"
fi

# ===================================
# 6. Service Health Checks
# ===================================
header "Service Health Checks"

# Backend health check
if docker ps | grep -q invmgmt-backend; then
    if docker exec invmgmt-backend curl -s http://localhost:5000/health > /dev/null 2>&1; then
        pass "Backend health check responding"
    else
        warn "Backend health check not responding (might still be starting)"
    fi
else
    warn "Backend not running (cannot check health)"
fi

# Frontend health check
if docker ps | grep -q invmgmt-frontend; then
    if docker exec invmgmt-frontend curl -s http://localhost/ > /dev/null 2>&1; then
        pass "Frontend responding"
    else
        warn "Frontend not responding (might still be starting)"
    fi
else
    warn "Frontend not running (cannot check health)"
fi

# Database connection
if docker ps | grep -q invmgmt-postgres; then
    if docker exec invmgmt-postgres pg_isready -U postgres > /dev/null 2>&1; then
        pass "PostgreSQL accepting connections"
    else
        warn "PostgreSQL not accepting connections"
    fi
else
    warn "PostgreSQL not running (cannot check)"
fi

# ===================================
# 7. File Structure
# ===================================
header "Project Structure"

# Dockerfiles
if [ -f "invmgmt.web/Dockerfile" ]; then
    pass "Backend Dockerfile found"
else
    fail "Backend Dockerfile not found"
fi

if [ -f "Invmgmt-master/Dockerfile" ]; then
    pass "Frontend Dockerfile found"
else
    fail "Frontend Dockerfile not found"
fi

# Key files
if [ -f "Invmgmt-master/nginx.default.conf" ]; then
    pass "Nginx configuration found"
else
    fail "Nginx configuration not found"
fi

if [ -f "invmgmt.web/Program.cs" ]; then
    pass "Backend entry point found"
else
    fail "Backend entry point not found"
fi

# ===================================
# 8. Logs
# ===================================
header "Recent Container Logs"

if [ "$BACKEND_COUNT" -gt 0 ]; then
    echo -e "\n${YELLOW}Backend logs (last 10 lines):${NC}"
    docker logs --tail 10 invmgmt-backend 2>/dev/null | head -10 || echo "  (no logs available)"
fi

if [ "$POSTGRES_COUNT" -gt 0 ]; then
    echo -e "\n${YELLOW}PostgreSQL logs (last 5 lines):${NC}"
    docker logs --tail 5 invmgmt-postgres 2>/dev/null | head -5 || echo "  (no logs available)"
fi

# ===================================
# Summary
# ===================================
header "Diagnostic Summary"

TOTAL=$((PASS_COUNT + FAIL_COUNT + WARN_COUNT))
echo -e "Total checks: $TOTAL"
echo -e "${GREEN}Passed: $PASS_COUNT${NC}"
echo -e "${RED}Failed: $FAIL_COUNT${NC}"
echo -e "${YELLOW}Warnings: $WARN_COUNT${NC}"

if [ "$FAIL_COUNT" -eq 0 ]; then
    echo -e "\n${GREEN}✓ System ready for deployment${NC}"
    if docker ps | grep -q invmgmt-backend; then
        echo -e "${BLUE}Services are running${NC}"
        echo -e "  Frontend: http://localhost/"
        echo -e "  Backend:  http://localhost:5000"
        echo -e "  API Docs: http://localhost:5000/swagger"
    else
        echo -e "${YELLOW}Services not running yet${NC}"
        echo -e "  Start with: docker-compose up -d"
    fi
else
    echo -e "\n${RED}✗ Fix errors above and rerun diagnostics${NC}"
    exit 1
fi
