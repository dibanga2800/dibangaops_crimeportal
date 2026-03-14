using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIPBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddAzurePersonIdToFaceEmbedding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Incident columns (ClassificationVersion, IncidentCategory, etc.) already exist from prior migrations

            migrationBuilder.CreateTable(
                name: "FaceEmbeddings",
                columns: table => new
                {
                    FaceEmbeddingId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OffenderId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    IncidentId = table.Column<int>(type: "int", nullable: true),
                    FileName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ModelId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Embedding = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BestMatchSimilarity = table.Column<double>(type: "float", nullable: true),
                    AzurePersonId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FaceEmbeddings", x => x.FaceEmbeddingId);
                });

            migrationBuilder.CreateTable(
                name: "StoreRiskScores",
                columns: table => new
                {
                    StoreRiskScoreId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    StoreId = table.Column<int>(type: "int", nullable: true),
                    SiteId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    StoreName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ForDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Score = table.Column<double>(type: "float", nullable: false),
                    Level = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ExpectedIncidentsMin = table.Column<int>(type: "int", nullable: true),
                    ExpectedIncidentsMax = table.Column<int>(type: "int", nullable: true),
                    PeakRiskWindows = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModelVersion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreRiskScores", x => x.StoreRiskScoreId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FaceEmbeddings_FileName",
                table: "FaceEmbeddings",
                column: "FileName");

            migrationBuilder.CreateIndex(
                name: "IX_FaceEmbeddings_IncidentId",
                table: "FaceEmbeddings",
                column: "IncidentId");

            migrationBuilder.CreateIndex(
                name: "IX_FaceEmbeddings_OffenderId",
                table: "FaceEmbeddings",
                column: "OffenderId");

            migrationBuilder.CreateIndex(
                name: "IX_StoreRiskScores_CustomerId_ForDate",
                table: "StoreRiskScores",
                columns: new[] { "CustomerId", "ForDate" });

            migrationBuilder.CreateIndex(
                name: "IX_StoreRiskScores_CustomerId_SiteId_ForDate",
                table: "StoreRiskScores",
                columns: new[] { "CustomerId", "SiteId", "ForDate" },
                unique: true,
                filter: "[SiteId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FaceEmbeddings");

            migrationBuilder.DropTable(
                name: "StoreRiskScores");
        }
    }
}
