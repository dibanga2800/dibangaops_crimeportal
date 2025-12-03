using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIPBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddSiteVisitTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SiteVisits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    ActionId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SiteVisitId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Customer = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    CustomerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Region = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    RegionName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SiteId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Location = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    LocationName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    VisitType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    OfficerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IdBadgeExpiry = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SiaLicenceNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SiaLicenceExpiry = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RecordOfIncidentsCompletion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DailyOccurrenceBookCompletion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PocketBookCompletion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    EcrCompletion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Top20Lines = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AssignmentInstructions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AssignmentInstructionsUnderstood = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    HealthAndSafetyUnderstood = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    HealthAndSafetyInPlace = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    DateHSRiskAssessment = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TrainingInstructionsGivenDate = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Jumper = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Shirt = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Tie = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    HiVisJacket = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Jacket = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Trousers = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Epaulettes = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Shoes = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TrainingInstructions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityOfficerSign = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ManagerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    FollowUpAction = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FollowUpActionDate = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AssignmentInstructionsInPlace = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    AssignmentInstructionsDate = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Recommendations = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    UpdatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteVisits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SiteVisits_AspNetUsers_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SiteVisits_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SiteVisits_AspNetUsers_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SiteVisits_AspNetUsers_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SiteVisits_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "CustomerId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SiteVisits_CreatedBy",
                table: "SiteVisits",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_SiteVisits_CreatedByUserId",
                table: "SiteVisits",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SiteVisits_CustomerId",
                table: "SiteVisits",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_SiteVisits_CustomerId_SiteId",
                table: "SiteVisits",
                columns: new[] { "CustomerId", "SiteId" });

            migrationBuilder.CreateIndex(
                name: "IX_SiteVisits_Date",
                table: "SiteVisits",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_SiteVisits_Status",
                table: "SiteVisits",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SiteVisits_UpdatedBy",
                table: "SiteVisits",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_SiteVisits_UpdatedByUserId",
                table: "SiteVisits",
                column: "UpdatedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SiteVisits");
        }
    }
}
