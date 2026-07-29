# Direct database update to clear active request for testing
# This is a temporary workaround for testing purposes

$connectionString = "Host=inveee-postgres.citg4maasb05.us-east-1.rds.amazonaws.com;Port=5432;Database=inventorydb;Username=postgres;Password=Inveee@123"

# Update request ID 3 from PendingAdminApproval to Received (terminal state)
$sql = "UPDATE ""Requests"" SET ""Status"" = 'Received', ""UpdatedAt"" = NOW() WHERE ""Id"" = 3 AND ""UserId"" = (SELECT ""Id"" FROM ""Users"" WHERE ""Email"" = 'ridhi@gmail.com');"

# Using psql to execute the SQL
$env:PGPASSWORD = "Inveee@123"
& psql -h inveee-postgres.citg4maasb05.us-east-1.rds.amazonaws.com -p 5432 -U postgres -d inventorydb -c $sql

Write-Host "Updated request ID 3 to Received status"
