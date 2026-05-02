using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace invmgmt.web.Migrations
{
    /// <inheritdoc />
    public partial class FixCategoryRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApprovalLogs_User_UserId",
                table: "ApprovalLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_IssueLogs_User_UserId",
                table: "IssueLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_ReceivedLogs_User_UserId",
                table: "ReceivedLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_RegistrationRequests_Departments_DepartmentId",
                table: "RegistrationRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_RegistrationRequests_User_ApprovedUserId",
                table: "RegistrationRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_Requests_User_UserId",
                table: "Requests");

            migrationBuilder.DropForeignKey(
                name: "FK_User_Departments_DepartmentId",
                table: "User");

            migrationBuilder.DropForeignKey(
                name: "FK_UserRoles_User_UserId",
                table: "UserRoles");

            migrationBuilder.DropIndex(
                name: "IX_InventoryStocks_ItemId",
                table: "InventoryStocks");

            migrationBuilder.DropPrimaryKey(
                name: "PK_User",
                table: "User");

            migrationBuilder.RenameTable(
                name: "User",
                newName: "Users");

            migrationBuilder.RenameIndex(
                name: "IX_User_DepartmentId",
                table: "Users",
                newName: "IX_Users_DepartmentId");

            migrationBuilder.AlterColumn<int>(
                name: "DepartmentId",
                table: "RegistrationRequests",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RoleId",
                table: "RegistrationRequests",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CategoryId1",
                table: "Items",
                type: "integer",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Users",
                table: "Users",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationRequests_RoleId",
                table: "RegistrationRequests",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Items_CategoryId1",
                table: "Items",
                column: "CategoryId1");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryStocks_ItemId",
                table: "InventoryStocks",
                column: "ItemId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ApprovalLogs_Users_UserId",
                table: "ApprovalLogs",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_IssueLogs_Users_UserId",
                table: "IssueLogs",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Items_Categories_CategoryId1",
                table: "Items",
                column: "CategoryId1",
                principalTable: "Categories",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ReceivedLogs_Users_UserId",
                table: "ReceivedLogs",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RegistrationRequests_Departments_DepartmentId",
                table: "RegistrationRequests",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RegistrationRequests_Roles_RoleId",
                table: "RegistrationRequests",
                column: "RoleId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RegistrationRequests_Users_ApprovedUserId",
                table: "RegistrationRequests",
                column: "ApprovedUserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Requests_Users_UserId",
                table: "Requests",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserRoles_Users_UserId",
                table: "UserRoles",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Departments_DepartmentId",
                table: "Users",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApprovalLogs_Users_UserId",
                table: "ApprovalLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_IssueLogs_Users_UserId",
                table: "IssueLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_Items_Categories_CategoryId1",
                table: "Items");

            migrationBuilder.DropForeignKey(
                name: "FK_ReceivedLogs_Users_UserId",
                table: "ReceivedLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_RegistrationRequests_Departments_DepartmentId",
                table: "RegistrationRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_RegistrationRequests_Roles_RoleId",
                table: "RegistrationRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_RegistrationRequests_Users_ApprovedUserId",
                table: "RegistrationRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_Requests_Users_UserId",
                table: "Requests");

            migrationBuilder.DropForeignKey(
                name: "FK_UserRoles_Users_UserId",
                table: "UserRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Departments_DepartmentId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_RegistrationRequests_RoleId",
                table: "RegistrationRequests");

            migrationBuilder.DropIndex(
                name: "IX_Items_CategoryId1",
                table: "Items");

            migrationBuilder.DropIndex(
                name: "IX_InventoryStocks_ItemId",
                table: "InventoryStocks");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Users",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "RoleId",
                table: "RegistrationRequests");

            migrationBuilder.DropColumn(
                name: "CategoryId1",
                table: "Items");

            migrationBuilder.RenameTable(
                name: "Users",
                newName: "User");

            migrationBuilder.RenameIndex(
                name: "IX_Users_DepartmentId",
                table: "User",
                newName: "IX_User_DepartmentId");

            migrationBuilder.AlterColumn<int>(
                name: "DepartmentId",
                table: "RegistrationRequests",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddPrimaryKey(
                name: "PK_User",
                table: "User",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryStocks_ItemId",
                table: "InventoryStocks",
                column: "ItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_ApprovalLogs_User_UserId",
                table: "ApprovalLogs",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_IssueLogs_User_UserId",
                table: "IssueLogs",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ReceivedLogs_User_UserId",
                table: "ReceivedLogs",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RegistrationRequests_Departments_DepartmentId",
                table: "RegistrationRequests",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RegistrationRequests_User_ApprovedUserId",
                table: "RegistrationRequests",
                column: "ApprovedUserId",
                principalTable: "User",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Requests_User_UserId",
                table: "Requests",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_User_Departments_DepartmentId",
                table: "User",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserRoles_User_UserId",
                table: "UserRoles",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
