using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIPBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingEmployeeFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DateDLChecked",
                table: "Employees",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FullRotasIssued",
                table: "Employees",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "GraydonCheckAuthorised",
                table: "Employees",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "GraydonCheckDetails",
                table: "Employees",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InductionAndTrainingBooked",
                table: "Employees",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "InitialOralReferencesComplete",
                table: "Employees",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "InitialOralReferencesDate",
                table: "Employees",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "Employees",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhotoFile",
                table: "Employees",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SixMonthlyCheck",
                table: "Employees",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Trainer",
                table: "Employees",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WorkingTimeDirective",
                table: "Employees",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DateDLChecked",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "FullRotasIssued",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "GraydonCheckAuthorised",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "GraydonCheckDetails",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "InductionAndTrainingBooked",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "InitialOralReferencesComplete",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "InitialOralReferencesDate",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "PhotoFile",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "SixMonthlyCheck",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "Trainer",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "WorkingTimeDirective",
                table: "Employees");
        }
    }
}
