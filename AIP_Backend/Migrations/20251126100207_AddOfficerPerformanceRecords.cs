using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIPBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddOfficerPerformanceRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OfficerPerformanceRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    OfficerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IncidentCount = table.Column<int>(type: "int", nullable: false),
                    ResolvedCount = table.Column<int>(type: "int", nullable: false),
                    TotalValueRecovered = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ResponseRate = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    SnapshotGeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfficerPerformanceRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OfficerPerformanceRecords_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "CustomerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OfficerPerformanceRecords_CustomerId_OfficerName",
                table: "OfficerPerformanceRecords",
                columns: new[] { "CustomerId", "OfficerName" });

            migrationBuilder.CreateIndex(
                name: "IX_OfficerPerformanceRecords_CustomerId_StartDate_EndDate_OfficerName",
                table: "OfficerPerformanceRecords",
                columns: new[] { "CustomerId", "StartDate", "EndDate", "OfficerName" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OfficerPerformanceRecords");
        }
    }
}
