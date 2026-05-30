using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace invmgmt.web.Migrations
{
    public partial class AddRoleColumnEF : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add Role column only if it doesn't already exist (safe for existing DBs)
            migrationBuilder.Sql(@"DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='public' AND table_name='Users' AND column_name='Role') THEN
        ALTER TABLE ""Users"" ADD COLUMN ""Role"" text NOT NULL DEFAULT 'USER';
    END IF;
END$$;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove Role column if present
            migrationBuilder.Sql(@"DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='public' AND table_name='Users' AND column_name='Role') THEN
        ALTER TABLE ""Users"" DROP COLUMN IF EXISTS ""Role"";
    END IF;
END$$;");
        }
    }
}
