using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace invmgmt.web.Migrations
{
    /// <inheritdoc />
    public partial class AddCompositeIndexRequestItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RequestItems_RequestId",
                table: "RequestItems");

            migrationBuilder.DropIndex(
                name: "IX_RequestItems_Status",
                table: "RequestItems");

            migrationBuilder.CreateIndex(
                name: "IX_RequestItems_RequestId_Status",
                table: "RequestItems",
                columns: new[] { "RequestId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RequestItems_RequestId_Status",
                table: "RequestItems");

            migrationBuilder.CreateIndex(
                name: "IX_RequestItems_RequestId",
                table: "RequestItems",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestItems_Status",
                table: "RequestItems",
                column: "Status");
        }
    }
}
