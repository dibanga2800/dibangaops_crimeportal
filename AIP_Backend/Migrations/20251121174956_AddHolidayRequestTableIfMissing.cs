using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIPBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddHolidayRequestTableIfMissing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HolidayRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReturnToWorkDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateOfRequest = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AuthorisedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    DateAuthorised = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TotalDays = table.Column<int>(type: "int", nullable: false),
                    Archived = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    UpdatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true)
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
                    table.ForeignKey(
                        name: "FK_HolidayRequests_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.Restrict);
                });

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
                name: "IX_HolidayRequests_EmployeeId",
                table: "HolidayRequests",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_HolidayRequests_EmployeeId_Status",
                table: "HolidayRequests",
                columns: new[] { "EmployeeId", "Status" });

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HolidayRequests");
        }
    }
}
