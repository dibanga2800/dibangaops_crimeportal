using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIPBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddDailyActivityReportsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DailyActivityReports",
                columns: table => new
                {
                    DailyActivityReportId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    CustomerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SiteId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SiteName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ReportDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OfficerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ShiftStart = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ShiftEnd = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    WeatherConditions = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ActivitiesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IncidentsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityChecksJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VisitorLogJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ComplianceJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InsecureAreasJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SystemsNotWorkingJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    RecordIsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyActivityReports", x => x.DailyActivityReportId);
                    table.ForeignKey(
                        name: "FK_DailyActivityReports_AspNetUsers_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DailyActivityReports_AspNetUsers_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DailyActivityReports_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "CustomerId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DailyActivityReports_CreatedBy",
                table: "DailyActivityReports",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_DailyActivityReports_CustomerId",
                table: "DailyActivityReports",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_DailyActivityReports_OfficerName",
                table: "DailyActivityReports",
                column: "OfficerName");

            migrationBuilder.CreateIndex(
                name: "IX_DailyActivityReports_ReportDate",
                table: "DailyActivityReports",
                column: "ReportDate");

            migrationBuilder.CreateIndex(
                name: "IX_DailyActivityReports_SiteId",
                table: "DailyActivityReports",
                column: "SiteId");

            migrationBuilder.CreateIndex(
                name: "IX_DailyActivityReports_UpdatedBy",
                table: "DailyActivityReports",
                column: "UpdatedBy");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DailyActivityReports");
        }
    }
}
