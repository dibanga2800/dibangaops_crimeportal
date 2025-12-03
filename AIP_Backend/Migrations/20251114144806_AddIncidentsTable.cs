using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIPBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddIncidentsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Incidents",
                columns: table => new
                {
                    IncidentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    SiteId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RegionId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SiteName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RegionName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Location = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OfficerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    OfficerRole = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OfficerType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DutyManagerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AssignedTo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DateOfIncident = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TimeOfIncident = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    DateInputted = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IncidentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ActionCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IncidentInvolvedJson = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: true),
                    IncidentDetails = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: true),
                    StoreComments = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: true),
                    TotalValueRecovered = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ValueRecovered = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    QuantityRecovered = table.Column<int>(type: "int", nullable: true),
                    UniformOfficer = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    StoreDetective = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    PoliceInvolvement = table.Column<bool>(type: "bit", nullable: false),
                    UrnNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CrimeRefNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PoliceId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Priority = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ActionTaken = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    EvidenceAttached = table.Column<bool>(type: "bit", nullable: false),
                    WitnessStatementsJson = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: true),
                    InvolvedPartiesJson = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ReportNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OffenderName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    OffenderSex = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Gender = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    OffenderDOB = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OffenderPlaceOfBirth = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    OffenderHouseName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OffenderNumberAndStreet = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    OffenderVillageOrSuburb = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OffenderTown = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OffenderCounty = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OffenderPostCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ArrestSaveComment = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RecordIsDeletedYN = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Incidents", x => x.IncidentId);
                    table.ForeignKey(
                        name: "FK_Incidents_AspNetUsers_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Incidents_AspNetUsers_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Incidents_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "CustomerId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StolenItems",
                columns: table => new
                {
                    StolenItemId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IncidentId = table.Column<int>(type: "int", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ProductName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Cost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Barcode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StolenItems", x => x.StolenItemId);
                    table.ForeignKey(
                        name: "FK_StolenItems_Incidents_IncidentId",
                        column: x => x.IncidentId,
                        principalTable: "Incidents",
                        principalColumn: "IncidentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_CreatedBy",
                table: "Incidents",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_CustomerId",
                table: "Incidents",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_DateOfIncident",
                table: "Incidents",
                column: "DateOfIncident");

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_IncidentType",
                table: "Incidents",
                column: "IncidentType");

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_SiteId",
                table: "Incidents",
                column: "SiteId");

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_Status",
                table: "Incidents",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_UpdatedBy",
                table: "Incidents",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_StolenItems_IncidentId",
                table: "StolenItems",
                column: "IncidentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StolenItems");

            migrationBuilder.DropTable(
                name: "Incidents");
        }
    }
}
