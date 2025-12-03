using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIPBackend.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSiteDateFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add new nullable DateTime columns
            migrationBuilder.AddColumn<DateTime>(
                name: "SiteSurveyComplete_New",
                table: "Sites",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RiskAssessmentIssued_New",
                table: "Sites",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AssignmentInstructionsIssued_New",
                table: "Sites",
                type: "datetime2",
                nullable: true);

            // Drop the old boolean columns
            migrationBuilder.DropColumn(
                name: "SiteSurveyComplete",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "RiskAssessmentIssued",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "AssignmentInstructionsIssued",
                table: "Sites");

            // Rename the new columns to the original names
            migrationBuilder.RenameColumn(
                name: "SiteSurveyComplete_New",
                table: "Sites",
                newName: "SiteSurveyComplete");

            migrationBuilder.RenameColumn(
                name: "RiskAssessmentIssued_New",
                table: "Sites",
                newName: "RiskAssessmentIssued");

            migrationBuilder.RenameColumn(
                name: "AssignmentInstructionsIssued_New",
                table: "Sites",
                newName: "AssignmentInstructionsIssued");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Add back the boolean columns
            migrationBuilder.AddColumn<bool>(
                name: "SiteSurveyComplete_Old",
                table: "Sites",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RiskAssessmentIssued_Old",
                table: "Sites",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AssignmentInstructionsIssued_Old",
                table: "Sites",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // Drop the DateTime columns
            migrationBuilder.DropColumn(
                name: "SiteSurveyComplete",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "RiskAssessmentIssued",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "AssignmentInstructionsIssued",
                table: "Sites");

            // Rename the boolean columns to the original names
            migrationBuilder.RenameColumn(
                name: "SiteSurveyComplete_Old",
                table: "Sites",
                newName: "SiteSurveyComplete");

            migrationBuilder.RenameColumn(
                name: "RiskAssessmentIssued_Old",
                table: "Sites",
                newName: "RiskAssessmentIssued");

            migrationBuilder.RenameColumn(
                name: "AssignmentInstructionsIssued_Old",
                table: "Sites",
                newName: "AssignmentInstructionsIssued");
        }
    }
}
