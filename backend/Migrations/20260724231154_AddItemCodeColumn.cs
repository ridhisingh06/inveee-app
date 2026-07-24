using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace invmgmt.web.Migrations
{
    /// <inheritdoc />
    public partial class AddItemCodeColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ItemId",
                schema: "public",
                table: "Items",
                newName: "ItemCode");

            migrationBuilder.AddColumn<int>(
                name: "MinimumQuantity",
                table: "InventoryStocks",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // First, update any duplicate or empty ItemCode values to ensure uniqueness
            migrationBuilder.Sql(
                @"UPDATE ""Items"" 
                  SET ""ItemCode"" = 'ITEM' || ""Id""::text 
                  WHERE ""ItemCode"" IS NULL OR ""ItemCode"" = '' OR ""ItemCode"" ~ '^[0-9]+$'");

            // Then add the unique constraint
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
            migrationBuilder.DropUniqueConstraint(
                name: "AK_Items_ItemCode",
                schema: "public",
                table: "Items");

            migrationBuilder.DropIndex(
                name: "IX_Items_ItemCode",
                schema: "public",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "MinimumQuantity",
                table: "InventoryStocks");

            migrationBuilder.RenameColumn(
                name: "ItemCode",
                schema: "public",
                table: "Items",
                newName: "ItemId");
        }
    }
}
