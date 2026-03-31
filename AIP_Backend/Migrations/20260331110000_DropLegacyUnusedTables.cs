using AIPBackend.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIPBackend.Migrations
{
	[DbContext(typeof(ApplicationDbContext))]
	[Migration("20260331110000_DropLegacyUnusedTables")]
	/// <inheritdoc />
	public partial class DropLegacyUnusedTables : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.Sql(
				@"
IF OBJECT_ID(N'[dbo].[CustomerSatisfactionSurveyFollowUpActions]', N'U') IS NOT NULL
	DROP TABLE [dbo].[CustomerSatisfactionSurveyFollowUpActions];

IF OBJECT_ID(N'[dbo].[CustomerSatisfactionSurveyDatesToBeCompleted]', N'U') IS NOT NULL
	DROP TABLE [dbo].[CustomerSatisfactionSurveyDatesToBeCompleted];

IF OBJECT_ID(N'[dbo].[CustomerSatisfactionSurveys]', N'U') IS NOT NULL
	DROP TABLE [dbo].[CustomerSatisfactionSurveys];

IF OBJECT_ID(N'[dbo].[ManagerSupportDeclarations]', N'U') IS NOT NULL
	DROP TABLE [dbo].[ManagerSupportDeclarations];

IF OBJECT_ID(N'[dbo].[ManagerSupportUpdates]', N'U') IS NOT NULL
	DROP TABLE [dbo].[ManagerSupportUpdates];

IF OBJECT_ID(N'[dbo].[OfficerSupportDeclarations]', N'U') IS NOT NULL
	DROP TABLE [dbo].[OfficerSupportDeclarations];

IF OBJECT_ID(N'[dbo].[OfficerSupportUpdates]', N'U') IS NOT NULL
	DROP TABLE [dbo].[OfficerSupportUpdates];

IF OBJECT_ID(N'[dbo].[MysteryShopperEvaluationScores]', N'U') IS NOT NULL
	DROP TABLE [dbo].[MysteryShopperEvaluationScores];

IF OBJECT_ID(N'[dbo].[MysteryShopperEvaluations]', N'U') IS NOT NULL
	DROP TABLE [dbo].[MysteryShopperEvaluations];

IF OBJECT_ID(N'[dbo].[OfficerPerformanceRecords]', N'U') IS NOT NULL
	DROP TABLE [dbo].[OfficerPerformanceRecords];
");
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			// Legacy tables intentionally not recreated.
		}
	}
}
