using AIPBackend.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIPBackend.Migrations
{
	[DbContext(typeof(ApplicationDbContext))]
	[Migration("20260518140000_DropSafeDuressCodeWordTables")]
	/// <inheritdoc />
	public partial class DropSafeDuressCodeWordTables : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.Sql(
				"""
				IF OBJECT_ID(N'[dbo].[CodeWordHistories]', N'U') IS NOT NULL
					DROP TABLE [dbo].[CodeWordHistories];

				IF OBJECT_ID(N'[dbo].[CodeWords]', N'U') IS NOT NULL
					DROP TABLE [dbo].[CodeWords];
				""");
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			// Safe/duress code word tables are not recreated.
		}
	}
}
