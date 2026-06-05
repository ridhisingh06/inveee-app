using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace invmgmt.web.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueConstraintToItemName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "public");

            migrationBuilder.RenameTable(
                name: "Items",
                newName: "Items",
                newSchema: "public");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "public",
                table: "Items",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateIndex(
                name: "IX_Items_Name",
                schema: "public",
                table: "Items",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Items_Name",
                schema: "public",
                table: "Items");

            migrationBuilder.RenameTable(
                name: "Items",
                schema: "public",
                newName: "Items");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Items",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);
        }
    }
}
