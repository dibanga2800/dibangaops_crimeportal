using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIPBackend.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLegacyHolidayAndOccurrenceModules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DailyOccurrenceBooks");

            migrationBuilder.DropTable(
                name: "HolidayRequests");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DailyOccurrenceBooks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    ReportedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CrimeReportCompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateCommenced = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Details = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OccurrenceCode = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    OccurrenceCodeDescription = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    OccurrenceDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OccurrenceTime = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    OfficerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ReportedByBadgeNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ReportedByName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ReportedByRole = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Signature = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SiteId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SiteName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    StoreName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    StoreNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyOccurrenceBooks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DailyOccurrenceBooks_AspNetUsers_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DailyOccurrenceBooks_AspNetUsers_ReportedById",
                        column: x => x.ReportedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DailyOccurrenceBooks_AspNetUsers_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DailyOccurrenceBooks_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "CustomerId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HolidayRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AuthorisedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    UpdatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Archived = table.Column<bool>(type: "bit", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    DateAuthorised = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateOfRequest = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DaysLeftYTD = table.Column<int>(type: "int", nullable: true),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ReturnToWorkDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TotalDays = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HolidayRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HolidayRequests_AspNetUsers_AuthorisedBy",
                        column: x => x.AuthorisedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HolidayRequests_AspNetUsers_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HolidayRequests_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_HolidayRequests_AspNetUsers_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HolidayRequests_AspNetUsers_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_DailyOccurrenceBooks_CreatedBy",
                table: "DailyOccurrenceBooks",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_DailyOccurrenceBooks_CustomerId",
                table: "DailyOccurrenceBooks",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_DailyOccurrenceBooks_CustomerId_SiteId",
                table: "DailyOccurrenceBooks",
                columns: new[] { "CustomerId", "SiteId" });

            migrationBuilder.CreateIndex(
                name: "IX_DailyOccurrenceBooks_OccurrenceCode",
                table: "DailyOccurrenceBooks",
                column: "OccurrenceCode");

            migrationBuilder.CreateIndex(
                name: "IX_DailyOccurrenceBooks_OccurrenceDate",
                table: "DailyOccurrenceBooks",
                column: "OccurrenceDate");

            migrationBuilder.CreateIndex(
                name: "IX_DailyOccurrenceBooks_ReportedById",
                table: "DailyOccurrenceBooks",
                column: "ReportedById");

            migrationBuilder.CreateIndex(
                name: "IX_DailyOccurrenceBooks_StoreNumber",
                table: "DailyOccurrenceBooks",
                column: "StoreNumber");

            migrationBuilder.CreateIndex(
                name: "IX_DailyOccurrenceBooks_UpdatedBy",
                table: "DailyOccurrenceBooks",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_HolidayRequests_Archived",
                table: "HolidayRequests",
                column: "Archived");

            migrationBuilder.CreateIndex(
                name: "IX_HolidayRequests_AuthorisedBy",
                table: "HolidayRequests",
                column: "AuthorisedBy");

            migrationBuilder.CreateIndex(
                name: "IX_HolidayRequests_CreatedBy",
                table: "HolidayRequests",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_HolidayRequests_CreatedByUserId",
                table: "HolidayRequests",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_HolidayRequests_StartDate",
                table: "HolidayRequests",
                column: "StartDate");

            migrationBuilder.CreateIndex(
                name: "IX_HolidayRequests_Status",
                table: "HolidayRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_HolidayRequests_UpdatedBy",
                table: "HolidayRequests",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_HolidayRequests_UpdatedByUserId",
                table: "HolidayRequests",
                column: "UpdatedByUserId");
        }
    }
}
