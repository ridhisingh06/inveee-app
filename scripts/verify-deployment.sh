#!/bin/bash
# Deployment Verification Script
# Usage: ./verify-deployment.sh

set -e

echo "================================"
echo "RDS Connection Fix - Verification"
echo "================================"
echo ""

# Color codes
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

check_result() {
    if [ $1 -eq 0 ]; then
        echo -e "${GREEN}✅ PASS${NC} - $2"
        return 0
    else
        echo -e "${RED}❌ FAIL${NC} - $2"
        return 1
    fi
}

# Section 1: Code Changes
echo ""
echo "Section 1: Code Changes"
echo "========================"

# Check if all files modified
echo "Checking modified files..."
git diff --quiet || check_result 1 "Uncommitted changes exist"
git diff --quiet || echo -e "${YELLOW}  Note: Use 'git add' and 'git commit' to save changes${NC}"

# Check specific changes
if grep -q "Database=inventorydb" invmgmt.web/appsettings.json; then
    check_result 0 "appsettings.json: Database=inventorydb"
else
    check_result 1 "appsettings.json: Database not fixed"
fi

if grep -q "Database=inventorydb" invmgmt.web/appsettings.Development.json; then
    check_result 0 "appsettings.Development.json: Database=inventorydb"
else
    check_result 1 "appsettings.Development.json: Database not fixed"
fi

if grep -q 'SSL Mode=Prefer' terraform/main.tf; then
    check_result 0 "terraform/main.tf: SSL Mode=Prefer"
else
    check_result 1 "terraform/main.tf: SSL Mode not fixed"
fi

if grep -q 'db_subnet_group_name' terraform/main.tf; then
    check_result 0 "terraform/main.tf: DB Subnet Group configured"
else
    check_result 1 "terraform/main.tf: DB Subnet Group missing"
fi

if grep -q 'depends_on = \[' terraform/main.tf; then
    check_result 0 "terraform/main.tf: ECS depends_on RDS"
else
    check_result 1 "terraform/main.tf: depends_on missing"
fi

if grep -q 'Connection String:' invmgmt.web/Program.cs; then
    check_result 0 "Program.cs: Connection string logging"
else
    check_result 1 "Program.cs: Logging not added"
fi

# Section 2: Local Environment
echo ""
echo "Section 2: Local Environment"
echo "=============================  "

# Check Docker
if command -v docker &> /dev/null; then
    check_result 0 "Docker is installed"
else
    check_result 1 "Docker is not installed"
fi

# Check if docker-compose file exists
if [ -f "docker-compose.yml" ]; then
    check_result 0 "docker-compose.yml exists"
else
    check_result 1 "docker-compose.yml not found"
fi

# Check .env file
if [ -f ".env" ]; then
    check_result 0 ".env file exists"
    
    # Check .env contents
    if grep -q "POSTGRES_DB" .env; then
        check_result 0 ".env: POSTGRES_DB configured"
    else
        check_result 1 ".env: POSTGRES_DB not configured"
    fi
else
    check_result 1 ".env file not found"
fi

# Section 3: AWS Configuration
echo ""
echo "Section 3: AWS Configuration"
echo "============================="

# Check AWS CLI
if command -v aws &> /dev/null; then
    check_result 0 "AWS CLI is installed"
    
    # Check AWS credentials
    if aws sts get-caller-identity &> /dev/null; then
        check_result 0 "AWS credentials are configured"
        ACCOUNT_ID=$(aws sts get-caller-identity --query Account --output text)
        echo "  AWS Account ID: $ACCOUNT_ID"
    else
        check_result 1 "AWS credentials not configured (run: aws configure)"
    fi
else
    check_result 1 "AWS CLI is not installed"
fi

# Check Terraform
if command -v terraform &> /dev/null; then
    check_result 0 "Terraform is installed"
    TF_VERSION=$(terraform version | head -1)
    echo "  Version: $TF_VERSION"
else
    check_result 1 "Terraform is not installed"
fi

# Section 4: Terraform Files
echo ""
echo "Section 4: Terraform Configuration"
echo "===================================="

if [ -d "terraform" ]; then
    check_result 0 "terraform/ directory exists"
    
    if [ -f "terraform/main.tf" ]; then
        check_result 0 "terraform/main.tf exists"
    else
        check_result 1 "terraform/main.tf not found"
    fi
    
    if [ -f "terraform/variables.tf" ]; then
        check_result 0 "terraform/variables.tf exists"
    else
        check_result 1 "terraform/variables.tf not found"
    fi
else
    check_result 1 "terraform/ directory not found"
fi

# Section 5: .NET Project
echo ""
echo "Section 5: .NET Project"
echo "======================="

if [ -f "invmgmt.web/invmgmt.web.csproj" ]; then
    check_result 0 "invmgmt.web.csproj exists"
else
    check_result 1 "invmgmt.web.csproj not found"
fi

if [ -f "invmgmt.web/Program.cs" ]; then
    check_result 0 "Program.cs exists"
else
    check_result 1 "Program.cs not found"
fi

if [ -f "invmgmt.web/Data/AppDbContext.cs" ]; then
    check_result 0 "AppDbContext.cs exists"
else
    check_result 1 "AppDbContext.cs not found"
fi

# Summary
echo ""
echo "================================"
echo "Verification Summary"
echo "================================"
echo ""
echo "Next Steps:"
echo "1. Commit changes: git commit -m 'Fix: RDS connection configuration'"
echo "2. Test locally: docker-compose up -d && docker-compose logs backend --follow"
echo "3. Deploy to AWS: cd terraform && terraform init && terraform plan && terraform apply"
echo "4. Verify: aws logs tail /ecs/inveee-app --follow --region ap-south-1"
echo ""
echo "Documentation:"
echo "- QUICKSTART_RDS_FIX.md (quick guide)"
echo "- SOLUTION_OVERVIEW.md (technical overview)"
echo "- AWS_RDS_CONNECTION_FIX.md (complete guide)"
echo "- DEPLOYMENT_READY_CHECKLIST.md (detailed checklist)"
echo ""
echo "================================"
echo "Ready for deployment! 🚀"
echo "================================"
