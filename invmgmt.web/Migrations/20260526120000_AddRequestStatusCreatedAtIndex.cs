using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace invmgmt.web.Migrations
{
    public partial class AddRequestStatusCreatedAtIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Requests_Status_CreatedAt",
                table: "Requests",
                columns: new[] { "Status", "CreatedAt" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Requests_Status_CreatedAt",
                table: "Requests");
        }
    }
}

