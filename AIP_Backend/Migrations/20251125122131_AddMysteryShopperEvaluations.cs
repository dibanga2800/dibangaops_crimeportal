using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIPBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddMysteryShopperEvaluations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MysteryShopperEvaluations",
                columns: table => new
                {
                    EvaluationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OfficerId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    OfficerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: true),
                    CustomerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SiteId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Location = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    LocationName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EvaluationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EvaluationTime = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    MysteryShopperName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TotalScore = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    MaxPossibleScore = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    Percentage = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RecordIsDeletedYN = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MysteryShopperEvaluations", x => x.EvaluationId);
                    table.ForeignKey(
                        name: "FK_MysteryShopperEvaluations_AspNetUsers_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MysteryShopperEvaluations_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MysteryShopperEvaluations_AspNetUsers_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MysteryShopperEvaluations_AspNetUsers_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MysteryShopperEvaluations_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "CustomerId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MysteryShopperEvaluationScores",
                columns: table => new
                {
                    ScoreId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EvaluationId = table.Column<int>(type: "int", nullable: false),
                    CriteriaId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Score = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MysteryShopperEvaluationScores", x => x.ScoreId);
                    table.ForeignKey(
                        name: "FK_MysteryShopperEvaluationScores_MysteryShopperEvaluations_EvaluationId",
                        column: x => x.EvaluationId,
                        principalTable: "MysteryShopperEvaluations",
                        principalColumn: "EvaluationId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MysteryShopperEvaluations_CreatedBy",
                table: "MysteryShopperEvaluations",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_MysteryShopperEvaluations_CreatedByUserId",
                table: "MysteryShopperEvaluations",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MysteryShopperEvaluations_CustomerId",
                table: "MysteryShopperEvaluations",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_MysteryShopperEvaluations_EvaluationDate",
                table: "MysteryShopperEvaluations",
                column: "EvaluationDate");

            migrationBuilder.CreateIndex(
                name: "IX_MysteryShopperEvaluations_OfficerId",
                table: "MysteryShopperEvaluations",
                column: "OfficerId");

            migrationBuilder.CreateIndex(
                name: "IX_MysteryShopperEvaluations_SiteId",
                table: "MysteryShopperEvaluations",
                column: "SiteId");

            migrationBuilder.CreateIndex(
                name: "IX_MysteryShopperEvaluations_Status",
                table: "MysteryShopperEvaluations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_MysteryShopperEvaluations_UpdatedBy",
                table: "MysteryShopperEvaluations",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_MysteryShopperEvaluations_UpdatedByUserId",
                table: "MysteryShopperEvaluations",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MysteryShopperEvaluationScores_EvaluationId",
                table: "MysteryShopperEvaluationScores",
                column: "EvaluationId");

            migrationBuilder.CreateIndex(
                name: "IX_MysteryShopperEvaluationScores_EvaluationId_CriteriaId",
                table: "MysteryShopperEvaluationScores",
                columns: new[] { "EvaluationId", "CriteriaId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MysteryShopperEvaluationScores");

            migrationBuilder.DropTable(
                name: "MysteryShopperEvaluations");
        }
    }
}
