using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIPBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddDailyOccurrenceBookTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DailyOccurrenceBooks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    SiteId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SiteName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    OccurrenceDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OccurrenceTime = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    OccurrenceType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ReportedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ReportedByName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ReportedByRole = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ReportedByBadgeNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    WitnessNamesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ActionTaken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FollowUpRequired = table.Column<bool>(type: "bit", nullable: false),
                    FollowUpBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    FollowUpDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FollowUpNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ManagerNotified = table.Column<bool>(type: "bit", nullable: false),
                    ManagerNotifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ManagerNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RelatedIncidentId = table.Column<int>(type: "int", nullable: true),
                    AttachmentsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
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
                        name: "FK_DailyOccurrenceBooks_AspNetUsers_FollowUpBy",
                        column: x => x.FollowUpBy,
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
                    table.ForeignKey(
                        name: "FK_DailyOccurrenceBooks_Incidents_RelatedIncidentId",
                        column: x => x.RelatedIncidentId,
                        principalTable: "Incidents",
                        principalColumn: "IncidentId",
                        onDelete: ReferentialAction.Restrict);
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
                name: "IX_DailyOccurrenceBooks_FollowUpBy",
                table: "DailyOccurrenceBooks",
                column: "FollowUpBy");

            migrationBuilder.CreateIndex(
                name: "IX_DailyOccurrenceBooks_OccurrenceDate",
                table: "DailyOccurrenceBooks",
                column: "OccurrenceDate");

            migrationBuilder.CreateIndex(
                name: "IX_DailyOccurrenceBooks_RelatedIncidentId",
                table: "DailyOccurrenceBooks",
                column: "RelatedIncidentId");

            migrationBuilder.CreateIndex(
                name: "IX_DailyOccurrenceBooks_ReportedById",
                table: "DailyOccurrenceBooks",
                column: "ReportedById");

            migrationBuilder.CreateIndex(
                name: "IX_DailyOccurrenceBooks_Severity",
                table: "DailyOccurrenceBooks",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_DailyOccurrenceBooks_Status",
                table: "DailyOccurrenceBooks",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_DailyOccurrenceBooks_UpdatedBy",
                table: "DailyOccurrenceBooks",
                column: "UpdatedBy");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DailyOccurrenceBooks");
        }
    }
}
