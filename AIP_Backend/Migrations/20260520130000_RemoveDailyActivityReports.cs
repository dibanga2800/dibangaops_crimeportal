using AIPBackend.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIPBackend.Migrations
{
	[DbContext(typeof(ApplicationDbContext))]
	[Migration("20260520130000_RemoveDailyActivityReports")]
	/// <inheritdoc />
	public partial class RemoveDailyActivityReports : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropTable(
				name: "DailyActivityReportActivities");

			migrationBuilder.DropTable(
				name: "DailyActivityReportIncidents");

			migrationBuilder.DropTable(
				name: "DailyActivityReportSecurityChecks");

			migrationBuilder.DropTable(
				name: "DailyActivityReportVisitorEntries");

			migrationBuilder.DropTable(
				name: "DailyActivityReports");
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			// Module removed from application; Down is intentionally not implemented.
		}
	}
}
