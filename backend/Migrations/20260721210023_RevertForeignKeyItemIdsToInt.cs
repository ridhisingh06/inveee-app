using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace invmgmt.web.Migrations
{
    /// <inheritdoc />
    public partial class RevertForeignKeyItemIdsToInt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ItemId",
                schema: "public",
                table: "Items",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ItemId",
                schema: "public",
                table: "Items");
        }
    }
}
