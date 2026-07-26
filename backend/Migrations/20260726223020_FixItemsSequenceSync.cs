using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace invmgmt.web.Migrations
{
    /// <inheritdoc />
    public partial class FixItemsSequenceSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Fix the Items_Id_seq sequence to sync with actual data
            migrationBuilder.Sql(
                @"DO $$
                DECLARE
                    max_id INTEGER;
                BEGIN
                    -- Get the current maximum Id from Items table
                    SELECT COALESCE(MAX(""Id""), 0) INTO max_id FROM ""Items"";
                    
                    -- Set the sequence to start from max_id + 1 to avoid conflicts
                    PERFORM setval('""Items_Id_seq""', max_id + 1, true);
                END $$;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
