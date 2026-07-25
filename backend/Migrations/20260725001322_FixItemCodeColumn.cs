using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace invmgmt.web.Migrations
{
    /// <inheritdoc />
    public partial class FixItemCodeColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop the incorrectly renamed ItemCode column (which was actually ItemId)
            migrationBuilder.DropColumn(
                name: "ItemCode",
                schema: "public",
                table: "Items");

            // Add the proper ItemCode column as string
            migrationBuilder.AddColumn<string>(
                name: "ItemCode",
                schema: "public",
                table: "Items",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            // Populate ItemCode with unique values based on Id
            migrationBuilder.Sql(
                @"UPDATE ""Items"" 
                  SET ""ItemCode"" = 'ITEM' || ""Id""::text 
                  WHERE ""ItemCode"" = ''");

            // Add unique constraint
            migrationBuilder.AddUniqueConstraint(
                name: "AK_Items_ItemCode",
                schema: "public",
                table: "Items",
                column: "ItemCode");

            migrationBuilder.CreateIndex(
                name: "IX_Items_ItemCode",
                schema: "public",
                table: "Items",
                column: "ItemCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
