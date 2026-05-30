using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace invmgmt.web.Migrations
{
    /// <inheritdoc />
    public partial class InitialFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Personnel",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ICNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    BirthDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    ResidentialAddress = table.Column<string>(type: "text", nullable: true),
                    ResidentialPhone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    OfficePhone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Designation = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    JobDescription = table.Column<string>(type: "text", nullable: true),
                    Department = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsStoresIncharge = table.Column<bool>(type: "boolean", nullable: false),
                    Building = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ReportingOfficer = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IdCardNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    IdCardExpiryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    PhotoPath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Personnel", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Personnel_Email",
                table: "Personnel",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Personnel");
        }
    }
}
