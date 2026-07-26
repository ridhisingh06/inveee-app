using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace invmgmt.web.Migrations
{
    /// <inheritdoc />
    public partial class FixItemsIdentityColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Fix the Items table Id column to ensure it has proper identity sequence
            migrationBuilder.Sql(
                @"DO $$
                DECLARE
                    max_id INTEGER;
                BEGIN
                    -- Check if sequence exists using pg_class for compatibility
                    IF NOT EXISTS (SELECT 1 FROM pg_class WHERE relname = 'Items_Id_seq' AND relkind = 'S') THEN
                        CREATE SEQUENCE ""Items_Id_seq"" 
                        AS integer
                        START WITH 1
                        INCREMENT BY 1
                        NO MINVALUE
                        NO MAXVALUE
                        CACHE 1;
                    END IF;
                    
                    -- Get the current maximum Id from Items table
                    SELECT COALESCE(MAX(""Id""), 0) INTO max_id FROM ""Items"";
                    
                    -- Set the sequence to start from max_id + 1 to avoid conflicts
                    PERFORM setval('""Items_Id_seq""', max_id + 1, true);
                    
                    -- Set the column default to use the sequence
                    ALTER TABLE ""Items"" 
                    ALTER COLUMN ""Id"" SET DEFAULT nextval('""Items_Id_seq""');
                    
                    -- Set sequence ownership
                    ALTER SEQUENCE ""Items_Id_seq"" OWNED BY ""Items"".""Id"";
                    
                    -- Ensure the column is NOT NULL
                    ALTER TABLE ""Items"" 
                    ALTER COLUMN ""Id"" SET NOT NULL;
                END $$;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
