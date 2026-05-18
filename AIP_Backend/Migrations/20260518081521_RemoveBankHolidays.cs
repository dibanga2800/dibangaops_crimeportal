using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIPBackend.Migrations
{
    /// <inheritdoc />
    public partial class RemoveBankHolidays : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BankHolidays");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BankHolidays",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Archived = table.Column<bool>(type: "bit", nullable: false),
                    AuthorisedByEmployeeId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateAuthorised = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateOfRequest = table.Column<DateTime>(type: "datetime2", nullable: false),
                    HolidayDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OfficerId = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankHolidays", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BankHolidays_AspNetUsers_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BankHolidays_AspNetUsers_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BankHolidays_Archived",
                table: "BankHolidays",
                column: "Archived");

            migrationBuilder.CreateIndex(
                name: "IX_BankHolidays_CreatedBy",
                table: "BankHolidays",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_BankHolidays_HolidayDate",
                table: "BankHolidays",
                column: "HolidayDate");

            migrationBuilder.CreateIndex(
                name: "IX_BankHolidays_OfficerId",
                table: "BankHolidays",
                column: "OfficerId");

            migrationBuilder.CreateIndex(
                name: "IX_BankHolidays_Status",
                table: "BankHolidays",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_BankHolidays_UpdatedBy",
                table: "BankHolidays",
                column: "UpdatedBy");
        }
    }
}
