using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIPBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddActionCalendar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ActionCalendars",
                columns: table => new
                {
                    ActionCalendarId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaskTitle = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    TaskDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TaskStatus = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PriorityLevel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AssignTo = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    IsRecurring = table.Column<bool>(type: "bit", nullable: false),
                    ReminderDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RecordIsDeletedYN = table.Column<bool>(type: "bit", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActionCalendars", x => x.ActionCalendarId);
                    table.ForeignKey(
                        name: "FK_ActionCalendars_AspNetUsers_AssignTo",
                        column: x => x.AssignTo,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ActionCalendars_AspNetUsers_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ActionCalendars_AspNetUsers_ModifiedBy",
                        column: x => x.ModifiedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ActionCalendarStatusUpdates",
                columns: table => new
                {
                    UpdateId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActionCalendarId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActionCalendarStatusUpdates", x => x.UpdateId);
                    table.ForeignKey(
                        name: "FK_ActionCalendarStatusUpdates_ActionCalendars_ActionCalendarId",
                        column: x => x.ActionCalendarId,
                        principalTable: "ActionCalendars",
                        principalColumn: "ActionCalendarId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ActionCalendarStatusUpdates_AspNetUsers_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActionCalendars_AssignTo",
                table: "ActionCalendars",
                column: "AssignTo");

            migrationBuilder.CreateIndex(
                name: "IX_ActionCalendars_CreatedBy",
                table: "ActionCalendars",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ActionCalendars_ModifiedBy",
                table: "ActionCalendars",
                column: "ModifiedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ActionCalendarStatusUpdates_ActionCalendarId",
                table: "ActionCalendarStatusUpdates",
                column: "ActionCalendarId");

            migrationBuilder.CreateIndex(
                name: "IX_ActionCalendarStatusUpdates_UpdatedBy",
                table: "ActionCalendarStatusUpdates",
                column: "UpdatedBy");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActionCalendarStatusUpdates");

            migrationBuilder.DropTable(
                name: "ActionCalendars");
        }
    }
}
