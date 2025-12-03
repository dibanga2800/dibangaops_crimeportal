using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIPBackend.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDailyOccurrenceBookSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DailyOccurrenceBooks_AspNetUsers_FollowUpBy",
                table: "DailyOccurrenceBooks");

            migrationBuilder.DropForeignKey(
                name: "FK_DailyOccurrenceBooks_Incidents_RelatedIncidentId",
                table: "DailyOccurrenceBooks");

            migrationBuilder.DropIndex(
                name: "IX_DailyOccurrenceBooks_FollowUpBy",
                table: "DailyOccurrenceBooks");

            migrationBuilder.DropIndex(
                name: "IX_DailyOccurrenceBooks_RelatedIncidentId",
                table: "DailyOccurrenceBooks");

            migrationBuilder.DropIndex(
                name: "IX_DailyOccurrenceBooks_Severity",
                table: "DailyOccurrenceBooks");

            migrationBuilder.DropIndex(
                name: "IX_DailyOccurrenceBooks_Status",
                table: "DailyOccurrenceBooks");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "DailyOccurrenceBooks",
                newName: "Details");

            migrationBuilder.DropColumn(
                name: "ActionTaken",
                table: "DailyOccurrenceBooks");

            migrationBuilder.DropColumn(
                name: "AttachmentsJson",
                table: "DailyOccurrenceBooks");

            migrationBuilder.DropColumn(
                name: "FollowUpBy",
                table: "DailyOccurrenceBooks");

            migrationBuilder.DropColumn(
                name: "FollowUpDate",
                table: "DailyOccurrenceBooks");

            migrationBuilder.DropColumn(
                name: "FollowUpNotes",
                table: "DailyOccurrenceBooks");

            migrationBuilder.DropColumn(
                name: "FollowUpRequired",
                table: "DailyOccurrenceBooks");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "DailyOccurrenceBooks");

            migrationBuilder.DropColumn(
                name: "ManagerNotified",
                table: "DailyOccurrenceBooks");

            migrationBuilder.DropColumn(
                name: "ManagerNotifiedAt",
                table: "DailyOccurrenceBooks");

            migrationBuilder.DropColumn(
                name: "ManagerNotes",
                table: "DailyOccurrenceBooks");

            migrationBuilder.DropColumn(
                name: "OccurrenceType",
                table: "DailyOccurrenceBooks");

            migrationBuilder.DropColumn(
                name: "RelatedIncidentId",
                table: "DailyOccurrenceBooks");

            migrationBuilder.DropColumn(
                name: "Severity",
                table: "DailyOccurrenceBooks");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "DailyOccurrenceBooks");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "DailyOccurrenceBooks");

            migrationBuilder.DropColumn(
                name: "WitnessNamesJson",
                table: "DailyOccurrenceBooks");

            migrationBuilder.AddColumn<DateTime>(
                name: "CrimeReportCompletedAt",
                table: "DailyOccurrenceBooks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateCommenced",
                table: "DailyOccurrenceBooks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OfficerName",
                table: "DailyOccurrenceBooks",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OccurrenceCode",
                table: "DailyOccurrenceBooks",
                type: "nvarchar(2)",
                maxLength: 2,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OccurrenceCodeDescription",
                table: "DailyOccurrenceBooks",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Signature",
                table: "DailyOccurrenceBooks",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StoreName",
                table: "DailyOccurrenceBooks",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StoreNumber",
                table: "DailyOccurrenceBooks",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DailyOccurrenceBooks_OccurrenceCode",
                table: "DailyOccurrenceBooks",
                column: "OccurrenceCode");

            migrationBuilder.CreateIndex(
                name: "IX_DailyOccurrenceBooks_StoreNumber",
                table: "DailyOccurrenceBooks",
                column: "StoreNumber");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DailyOccurrenceBooks_OccurrenceCode",
                table: "DailyOccurrenceBooks");

            migrationBuilder.DropIndex(
                name: "IX_DailyOccurrenceBooks_StoreNumber",
                table: "DailyOccurrenceBooks");

            migrationBuilder.DropColumn(
                name: "CrimeReportCompletedAt",
                table: "DailyOccurrenceBooks");

            migrationBuilder.DropColumn(
                name: "DateCommenced",
                table: "DailyOccurrenceBooks");

            migrationBuilder.DropColumn(
                name: "OfficerName",
                table: "DailyOccurrenceBooks");

            migrationBuilder.DropColumn(
                name: "OccurrenceCode",
                table: "DailyOccurrenceBooks");

            migrationBuilder.DropColumn(
                name: "OccurrenceCodeDescription",
                table: "DailyOccurrenceBooks");

            migrationBuilder.DropColumn(
                name: "Signature",
                table: "DailyOccurrenceBooks");

            migrationBuilder.DropColumn(
                name: "StoreName",
                table: "DailyOccurrenceBooks");

            migrationBuilder.DropColumn(
                name: "StoreNumber",
                table: "DailyOccurrenceBooks");

            migrationBuilder.RenameColumn(
                name: "Details",
                table: "DailyOccurrenceBooks",
                newName: "Description");

            migrationBuilder.AddColumn<string>(
                name: "ActionTaken",
                table: "DailyOccurrenceBooks",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AttachmentsJson",
                table: "DailyOccurrenceBooks",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FollowUpBy",
                table: "DailyOccurrenceBooks",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FollowUpDate",
                table: "DailyOccurrenceBooks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FollowUpNotes",
                table: "DailyOccurrenceBooks",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "FollowUpRequired",
                table: "DailyOccurrenceBooks",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "DailyOccurrenceBooks",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "ManagerNotified",
                table: "DailyOccurrenceBooks",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ManagerNotifiedAt",
                table: "DailyOccurrenceBooks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ManagerNotes",
                table: "DailyOccurrenceBooks",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OccurrenceType",
                table: "DailyOccurrenceBooks",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "RelatedIncidentId",
                table: "DailyOccurrenceBooks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Severity",
                table: "DailyOccurrenceBooks",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "DailyOccurrenceBooks",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "DailyOccurrenceBooks",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "WitnessNamesJson",
                table: "DailyOccurrenceBooks",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DailyOccurrenceBooks_FollowUpBy",
                table: "DailyOccurrenceBooks",
                column: "FollowUpBy");

            migrationBuilder.CreateIndex(
                name: "IX_DailyOccurrenceBooks_RelatedIncidentId",
                table: "DailyOccurrenceBooks",
                column: "RelatedIncidentId");

            migrationBuilder.CreateIndex(
                name: "IX_DailyOccurrenceBooks_Severity",
                table: "DailyOccurrenceBooks",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_DailyOccurrenceBooks_Status",
                table: "DailyOccurrenceBooks",
                column: "Status");

            migrationBuilder.AddForeignKey(
                name: "FK_DailyOccurrenceBooks_AspNetUsers_FollowUpBy",
                table: "DailyOccurrenceBooks",
                column: "FollowUpBy",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DailyOccurrenceBooks_Incidents_RelatedIncidentId",
                table: "DailyOccurrenceBooks",
                column: "RelatedIncidentId",
                principalTable: "Incidents",
                principalColumn: "IncidentId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}

