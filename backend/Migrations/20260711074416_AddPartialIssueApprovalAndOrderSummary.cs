using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace invmgmt.web.Migrations
{
    /// <inheritdoc />
    public partial class AddPartialIssueApprovalAndOrderSummary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ApprovedBy",
                table: "Requests",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedDate",
                table: "Requests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IssuedBy",
                table: "Requests",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "IssuedDate",
                table: "Requests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReceivedDate",
                table: "Requests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AdminApprovedQuantity",
                table: "RequestItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AdminRejectedQuantity",
                table: "RequestItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ApprovedBy",
                table: "RequestItems",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedDate",
                table: "RequestItems",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ConcurrencyToken",
                table: "RequestItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "IssuedBy",
                table: "RequestItems",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "IssuedDate",
                table: "RequestItems",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IssuerIssuedQuantity",
                table: "RequestItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "IssuerRejectedQuantity",
                table: "RequestItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReceivedDate",
                table: "RequestItems",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReceivedQuantity",
                table: "RequestItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "OrderSummaries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RequestId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    IssuedByUserId = table.Column<int>(type: "integer", nullable: true),
                    ApprovedByUserId = table.Column<int>(type: "integer", nullable: true),
                    RequestedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IssuedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ApprovedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReceivedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TotalRequestedQuantity = table.Column<int>(type: "integer", nullable: false),
                    TotalIssuedQuantity = table.Column<int>(type: "integer", nullable: false),
                    TotalApprovedQuantity = table.Column<int>(type: "integer", nullable: false),
                    TotalRejectedQuantity = table.Column<int>(type: "integer", nullable: false),
                    TotalReceivedQuantity = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderSummaries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderSummaries_Requests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "Requests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderSummaries_Users_ApprovedByUserId",
                        column: x => x.ApprovedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_OrderSummaries_Users_IssuedByUserId",
                        column: x => x.IssuedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_OrderSummaries_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrderSummaryItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderSummaryId = table.Column<int>(type: "integer", nullable: false),
                    ItemId = table.Column<int>(type: "integer", nullable: false),
                    RequestedQuantity = table.Column<int>(type: "integer", nullable: false),
                    IssuedQuantity = table.Column<int>(type: "integer", nullable: false),
                    IssuerRejectedQuantity = table.Column<int>(type: "integer", nullable: false),
                    ApprovedQuantity = table.Column<int>(type: "integer", nullable: false),
                    AdminRejectedQuantity = table.Column<int>(type: "integer", nullable: false),
                    ReceivedQuantity = table.Column<int>(type: "integer", nullable: false),
                    RequestItemId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderSummaryItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderSummaryItems_Items_ItemId",
                        column: x => x.ItemId,
                        principalSchema: "public",
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderSummaryItems_OrderSummaries_OrderSummaryId",
                        column: x => x.OrderSummaryId,
                        principalTable: "OrderSummaries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderSummaryItems_RequestItems_RequestItemId",
                        column: x => x.RequestItemId,
                        principalTable: "RequestItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderSummaries_ApprovedByUserId",
                table: "OrderSummaries",
                column: "ApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderSummaries_IssuedByUserId",
                table: "OrderSummaries",
                column: "IssuedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderSummaries_ReceivedDate",
                table: "OrderSummaries",
                column: "ReceivedDate");

            migrationBuilder.CreateIndex(
                name: "IX_OrderSummaries_RequestId",
                table: "OrderSummaries",
                column: "RequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderSummaries_Status",
                table: "OrderSummaries",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_OrderSummaries_UserId",
                table: "OrderSummaries",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderSummaryItems_ItemId",
                table: "OrderSummaryItems",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderSummaryItems_OrderSummaryId",
                table: "OrderSummaryItems",
                column: "OrderSummaryId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderSummaryItems_RequestItemId",
                table: "OrderSummaryItems",
                column: "RequestItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderSummaryItems");

            migrationBuilder.DropTable(
                name: "OrderSummaries");

            migrationBuilder.DropColumn(
                name: "ApprovedBy",
                table: "Requests");

            migrationBuilder.DropColumn(
                name: "ApprovedDate",
                table: "Requests");

            migrationBuilder.DropColumn(
                name: "IssuedBy",
                table: "Requests");

            migrationBuilder.DropColumn(
                name: "IssuedDate",
                table: "Requests");

            migrationBuilder.DropColumn(
                name: "ReceivedDate",
                table: "Requests");

            migrationBuilder.DropColumn(
                name: "AdminApprovedQuantity",
                table: "RequestItems");

            migrationBuilder.DropColumn(
                name: "AdminRejectedQuantity",
                table: "RequestItems");

            migrationBuilder.DropColumn(
                name: "ApprovedBy",
                table: "RequestItems");

            migrationBuilder.DropColumn(
                name: "ApprovedDate",
                table: "RequestItems");

            migrationBuilder.DropColumn(
                name: "ConcurrencyToken",
                table: "RequestItems");

            migrationBuilder.DropColumn(
                name: "IssuedBy",
                table: "RequestItems");

            migrationBuilder.DropColumn(
                name: "IssuedDate",
                table: "RequestItems");

            migrationBuilder.DropColumn(
                name: "IssuerIssuedQuantity",
                table: "RequestItems");

            migrationBuilder.DropColumn(
                name: "IssuerRejectedQuantity",
                table: "RequestItems");

            migrationBuilder.DropColumn(
                name: "ReceivedDate",
                table: "RequestItems");

            migrationBuilder.DropColumn(
                name: "ReceivedQuantity",
                table: "RequestItems");
        }
    }
}
