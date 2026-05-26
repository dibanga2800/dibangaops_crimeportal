using AIPBackend.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIPBackend.Migrations
{
	[DbContext(typeof(ApplicationDbContext))]
	[Migration("20260520140000_EnsureDailyOccurrenceBooksRemoved")]
	/// <inheritdoc />
	public partial class EnsureDailyOccurrenceBooksRemoved : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			// Idempotent: DailyOccurrenceBooks was dropped in 20260518080051; this covers DBs that lagged.
			migrationBuilder.Sql(
				"""
				IF OBJECT_ID(N'[dbo].[DailyOccurrenceBooks]', N'U') IS NOT NULL
				BEGIN
					DROP TABLE [dbo].[DailyOccurrenceBooks];
				END
				""");
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			// Module removed from application; Down is intentionally not implemented.
		}
	}
}
