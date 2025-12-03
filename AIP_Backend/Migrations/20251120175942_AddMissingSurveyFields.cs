using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIPBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingSurveyFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DatesToBeCompletedJson",
                table: "CustomerSatisfactionSurveys");

            migrationBuilder.DropColumn(
                name: "FollowUpActionsJson",
                table: "CustomerSatisfactionSurveys");

            migrationBuilder.AddColumn<string>(
                name: "PrimaryFollowUp",
                table: "CustomerSatisfactionSurveys",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrimaryFollowUpCompletionDate",
                table: "CustomerSatisfactionSurveys",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SecondaryFollowUp",
                table: "CustomerSatisfactionSurveys",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SecondaryFollowUpCompletionDate",
                table: "CustomerSatisfactionSurveys",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SiteName",
                table: "CustomerSatisfactionSurveys",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CustomerSatisfactionSurveyDatesToBeCompleted",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SurveyId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Date = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerSatisfactionSurveyDatesToBeCompleted", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerSatisfactionSurveyDatesToBeCompleted_CustomerSatisfactionSurveys_SurveyId",
                        column: x => x.SurveyId,
                        principalTable: "CustomerSatisfactionSurveys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CustomerSatisfactionSurveyFollowUpActions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SurveyId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerSatisfactionSurveyFollowUpActions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerSatisfactionSurveyFollowUpActions_CustomerSatisfactionSurveys_SurveyId",
                        column: x => x.SurveyId,
                        principalTable: "CustomerSatisfactionSurveys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerSatisfactionSurveyDatesToBeCompleted_SurveyId",
                table: "CustomerSatisfactionSurveyDatesToBeCompleted",
                column: "SurveyId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerSatisfactionSurveyFollowUpActions_SurveyId",
                table: "CustomerSatisfactionSurveyFollowUpActions",
                column: "SurveyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerSatisfactionSurveyDatesToBeCompleted");

            migrationBuilder.DropTable(
                name: "CustomerSatisfactionSurveyFollowUpActions");

            migrationBuilder.DropColumn(
                name: "PrimaryFollowUp",
                table: "CustomerSatisfactionSurveys");

            migrationBuilder.DropColumn(
                name: "PrimaryFollowUpCompletionDate",
                table: "CustomerSatisfactionSurveys");

            migrationBuilder.DropColumn(
                name: "SecondaryFollowUp",
                table: "CustomerSatisfactionSurveys");

            migrationBuilder.DropColumn(
                name: "SecondaryFollowUpCompletionDate",
                table: "CustomerSatisfactionSurveys");

            migrationBuilder.DropColumn(
                name: "SiteName",
                table: "CustomerSatisfactionSurveys");

            migrationBuilder.AddColumn<string>(
                name: "DatesToBeCompletedJson",
                table: "CustomerSatisfactionSurveys",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FollowUpActionsJson",
                table: "CustomerSatisfactionSurveys",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
