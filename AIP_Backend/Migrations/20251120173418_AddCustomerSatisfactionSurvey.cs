using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIPBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerSatisfactionSurvey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CustomerSatisfactionSurveys",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    OfficerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Customer = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Region = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Location = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RatingsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StoreManagerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AreaManagerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    FollowUpActionsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DatesToBeCompletedJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerSatisfactionSurveys", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerSatisfactionSurveys_AspNetUsers_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerSatisfactionSurveys_AspNetUsers_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerSatisfactionSurveys_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "CustomerId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerSatisfactionSurveys_CreatedBy",
                table: "CustomerSatisfactionSurveys",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerSatisfactionSurveys_CustomerId",
                table: "CustomerSatisfactionSurveys",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerSatisfactionSurveys_CustomerId_Date",
                table: "CustomerSatisfactionSurveys",
                columns: new[] { "CustomerId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerSatisfactionSurveys_Date",
                table: "CustomerSatisfactionSurveys",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerSatisfactionSurveys_UpdatedBy",
                table: "CustomerSatisfactionSurveys",
                column: "UpdatedBy");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerSatisfactionSurveys");
        }
    }
}
