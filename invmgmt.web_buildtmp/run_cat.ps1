$env:PGPASSWORD="ridhi@608"
psql -U postgres -d InvMgmtDb -h localhost -f cat_query.sql
