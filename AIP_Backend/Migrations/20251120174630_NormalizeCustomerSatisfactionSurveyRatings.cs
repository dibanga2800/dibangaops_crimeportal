using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIPBackend.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeCustomerSatisfactionSurveyRatings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RatingsJson",
                table: "CustomerSatisfactionSurveys");

            migrationBuilder.AddColumn<int>(
                name: "CustomerServiceApproach",
                table: "CustomerSatisfactionSurveys",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ImprovedFeelingOfSecurityWhenOfficerOnSite",
                table: "CustomerSatisfactionSurveys",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Proactivity",
                table: "CustomerSatisfactionSurveys",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Professionalism",
                table: "CustomerSatisfactionSurveys",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PunctualityBreaks",
                table: "CustomerSatisfactionSurveys",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RelationsWithStoreColleagues",
                table: "CustomerSatisfactionSurveys",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UniformAndAppearance",
                table: "CustomerSatisfactionSurveys",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomerServiceApproach",
                table: "CustomerSatisfactionSurveys");

            migrationBuilder.DropColumn(
                name: "ImprovedFeelingOfSecurityWhenOfficerOnSite",
                table: "CustomerSatisfactionSurveys");

            migrationBuilder.DropColumn(
                name: "Proactivity",
                table: "CustomerSatisfactionSurveys");

            migrationBuilder.DropColumn(
                name: "Professionalism",
                table: "CustomerSatisfactionSurveys");

            migrationBuilder.DropColumn(
                name: "PunctualityBreaks",
                table: "CustomerSatisfactionSurveys");

            migrationBuilder.DropColumn(
                name: "RelationsWithStoreColleagues",
                table: "CustomerSatisfactionSurveys");

            migrationBuilder.DropColumn(
                name: "UniformAndAppearance",
                table: "CustomerSatisfactionSurveys");

            migrationBuilder.AddColumn<string>(
                name: "RatingsJson",
                table: "CustomerSatisfactionSurveys",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
