using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIPBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddAlertInstancesAndEvidenceTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AlertInstances",
                columns: table => new
                {
                    AlertInstanceId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AlertRuleId = table.Column<int>(type: "int", nullable: false),
                    IncidentId = table.Column<int>(type: "int", nullable: true),
                    Severity = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    MatchDetails = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AcknowledgedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AcknowledgedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    EscalatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EscalatedTo = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolvedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ResolutionNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    EscalationLevel = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertInstances", x => x.AlertInstanceId);
                    table.ForeignKey(
                        name: "FK_AlertInstances_AlertRules_AlertRuleId",
                        column: x => x.AlertRuleId,
                        principalTable: "AlertRules",
                        principalColumn: "AlertRuleId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AlertInstances_Incidents_IncidentId",
                        column: x => x.IncidentId,
                        principalTable: "Incidents",
                        principalColumn: "IncidentId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EvidenceItems",
                columns: table => new
                {
                    EvidenceItemId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IncidentId = table.Column<int>(type: "int", nullable: false),
                    Barcode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EvidenceType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    StorageLocation = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RegisteredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RegisteredBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvidenceItems", x => x.EvidenceItemId);
                    table.ForeignKey(
                        name: "FK_EvidenceItems_Incidents_IncidentId",
                        column: x => x.IncidentId,
                        principalTable: "Incidents",
                        principalColumn: "IncidentId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EvidenceCustodyEvents",
                columns: table => new
                {
                    CustodyEventId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EvidenceItemId = table.Column<int>(type: "int", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Location = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    EventTimestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PerformedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    PerformedByName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvidenceCustodyEvents", x => x.CustodyEventId);
                    table.ForeignKey(
                        name: "FK_EvidenceCustodyEvents_EvidenceItems_EvidenceItemId",
                        column: x => x.EvidenceItemId,
                        principalTable: "EvidenceItems",
                        principalColumn: "EvidenceItemId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AlertInstances_AlertRuleId",
                table: "AlertInstances",
                column: "AlertRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_AlertInstances_CreatedAt",
                table: "AlertInstances",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AlertInstances_IncidentId",
                table: "AlertInstances",
                column: "IncidentId");

            migrationBuilder.CreateIndex(
                name: "IX_AlertInstances_Status",
                table: "AlertInstances",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceCustodyEvents_EvidenceItemId",
                table: "EvidenceCustodyEvents",
                column: "EvidenceItemId");

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceItems_Barcode",
                table: "EvidenceItems",
                column: "Barcode");

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceItems_IncidentId",
                table: "EvidenceItems",
                column: "IncidentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlertInstances");

            migrationBuilder.DropTable(
                name: "EvidenceCustodyEvents");

            migrationBuilder.DropTable(
                name: "EvidenceItems");
        }
    }
}
