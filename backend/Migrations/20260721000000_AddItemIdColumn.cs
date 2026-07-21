using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace invmgmt.web.Migrations
{
    /// <inheritdoc />
    public partial class AddItemIdColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add ItemId column to Items table
            migrationBuilder.AddColumn<string>(
                name: "ItemId",
                schema: "public",
                table: "Items",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            // Create unique index on ItemId
            migrationBuilder.CreateIndex(
                name: "IX_Items_ItemId",
                schema: "public",
                table: "Items",
                column: "ItemId",
                unique: true);

            // Update InventoryStocks.ItemId from int to string
            migrationBuilder.DropIndex(
                name: "IX_InventoryStocks_ItemId",
                schema: "public",
                table: "InventoryStocks");

            migrationBuilder.AlterColumn<string>(
                name: "ItemId",
                schema: "public",
                table: "InventoryStocks",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(int),
                oldType: "integer");

            // Create index on the new string ItemId column
            migrationBuilder.CreateIndex(
                name: "IX_InventoryStocks_ItemId",
                schema: "public",
                table: "InventoryStocks",
                column: "ItemId");

            // Update RequestItems.ItemId from int to string
            migrationBuilder.DropIndex(
                name: "IX_RequestItems_ItemId",
                schema: "public",
                table: "RequestItems");

            migrationBuilder.AlterColumn<string>(
                name: "ItemId",
                schema: "public",
                table: "RequestItems",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(int),
                oldType: "integer");

            // Create index on the new string ItemId column
            migrationBuilder.CreateIndex(
                name: "IX_RequestItems_ItemId",
                schema: "public",
                table: "RequestItems",
                column: "ItemId");

            // Migrate existing data: set ItemId to the string representation of Id for existing records
            migrationBuilder.Sql(
                @"UPDATE ""Items"" SET ""ItemId"" = ""Id""::text WHERE ""ItemId"" = ''");
            
            // Update InventoryStocks to use the new ItemId values
            migrationBuilder.Sql(
                @"UPDATE ""InventoryStocks"" 
                  SET ""ItemId"" = (SELECT ""ItemId"" FROM ""Items"" WHERE ""Items"".""Id"" = ""InventoryStocks"".""ItemId""::int)
                  WHERE ""ItemId"" ~ '^[0-9]+$'");

            // Update RequestItems to use the new ItemId values
            migrationBuilder.Sql(
                @"UPDATE ""RequestItems"" 
                  SET ""ItemId"" = (SELECT ""ItemId"" FROM ""Items"" WHERE ""Items"".""Id"" = ""RequestItems"".""ItemId""::int)
                  WHERE ""ItemId"" ~ '^[0-9]+$'");

            // Update OrderSummaryItems.ItemId from int to string
            migrationBuilder.DropIndex(
                name: "IX_OrderSummaryItems_ItemId",
                schema: "public",
                table: "OrderSummaryItems");

            migrationBuilder.AlterColumn<string>(
                name: "ItemId",
                schema: "public",
                table: "OrderSummaryItems",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(int),
                oldType: "integer");

            // Create index on the new string ItemId column
            migrationBuilder.CreateIndex(
                name: "IX_OrderSummaryItems_ItemId",
                schema: "public",
                table: "OrderSummaryItems",
                column: "ItemId");

            // Update OrderSummaryItems to use the new ItemId values
            migrationBuilder.Sql(
                @"UPDATE ""OrderSummaryItems"" 
                  SET ""ItemId"" = (SELECT ""ItemId"" FROM ""Items"" WHERE ""Items"".""Id"" = ""OrderSummaryItems"".""ItemId""::int)
                  WHERE ""ItemId"" ~ '^[0-9]+$'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop the unique index on ItemId
            migrationBuilder.DropIndex(
                name: "IX_Items_ItemId",
                schema: "public",
                table: "Items");

            // Drop ItemId column from Items table
            migrationBuilder.DropColumn(
                name: "ItemId",
                schema: "public",
                table: "Items");

            // Revert InventoryStocks.ItemId back to int
            migrationBuilder.DropIndex(
                name: "IX_InventoryStocks_ItemId",
                schema: "public",
                table: "InventoryStocks");

            migrationBuilder.AlterColumn<int>(
                name: "ItemId",
                schema: "public",
                table: "InventoryStocks",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            // Recreate index on the int ItemId column
            migrationBuilder.CreateIndex(
                name: "IX_InventoryStocks_ItemId",
                schema: "public",
                table: "InventoryStocks",
                column: "ItemId");

            // Revert RequestItems.ItemId back to int
            migrationBuilder.DropIndex(
                name: "IX_RequestItems_ItemId",
                schema: "public",
                table: "RequestItems");

            migrationBuilder.AlterColumn<int>(
                name: "ItemId",
                schema: "public",
                table: "RequestItems",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            // Recreate index on the int ItemId column
            migrationBuilder.CreateIndex(
                name: "IX_RequestItems_ItemId",
                schema: "public",
                table: "RequestItems",
                column: "ItemId");

            // Revert OrderSummaryItems.ItemId back to int
            migrationBuilder.DropIndex(
                name: "IX_OrderSummaryItems_ItemId",
                schema: "public",
                table: "OrderSummaryItems");

            migrationBuilder.AlterColumn<int>(
                name: "ItemId",
                schema: "public",
                table: "OrderSummaryItems",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            // Recreate index on the int ItemId column
            migrationBuilder.CreateIndex(
                name: "IX_OrderSummaryItems_ItemId",
                schema: "public",
                table: "OrderSummaryItems",
                column: "ItemId");
        }
    }
}
